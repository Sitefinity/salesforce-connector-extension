using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services.Protocols;
using Telerik.OpenAccess.DataAdapter.Metadata;
using Telerik.OpenAccess.DataAdapter.SalesForce.Data;
using Telerik.OpenAccess.DataAdapter.SalesForce.Metadata;
using Telerik.OpenAccess.DataAdapter.SalesForce.SaleforceService;
using Telerik.OpenAccess.DataAdapter.SalesForce.Util;
using Telerik.OpenAccess.DataAdapter.Sync;

namespace Sitefinity.SalesforceConnector.Extension
{
    public class SalesForceMetadataAdapter : IMetadataAdapter
    {
        internal const string ContentDocument = "ContentDocument";
        internal const string ContentVersion = "ContentVersion";
        private const string SystemModstamp = "SystemModstamp";
        private const string ContentSize = "ContentSize";

        private static readonly List<string> contentVersionFields = new List<string> { "Title", "ContentSize", "Description", "ReasonForChange", "VersionData", "PathOnClient", "FirstPublishLocationId" };

        public static IMetadataAdapter CreateInstance(AdapterConfiguration adapterConfiguration)
        {
            return new SalesForceMetadataAdapter(adapterConfiguration);
        }

        readonly AdapterConfiguration config;
        string username;
        string password;
        SforceService binding;
        private DefaultMapCapabilities mappingCapabilities;

        private List<string> objectNames;
        private List<MetaObject> libraries;

        public SalesForceMetadataAdapter(AdapterConfiguration adapterConfiguration)
        {
            if (adapterConfiguration == null)
                throw new ArgumentNullException("adapterConfiguration");

            if (adapterConfiguration.ConnectionString == null)
                throw new ArgumentNullException("adapterConfiguration.ConnectionString");

            this.config = adapterConfiguration;
            ProcessConfig();
        }

        public string[] GetListNames()
        {
            string error = Login();
            if (string.IsNullOrEmpty(error) == false)
                throw new Exception(error);

            this.objectNames = new List<string>();
            try
            {
                this.objectNames = DescribeGlobal();
                List<string> allNames = new List<string>(this.objectNames);
                //try and get the list of library names from the ContentWorkspace table
                if (allNames.Contains("ContentWorkspace"))
                {
                    this.libraries = GetLibraries();
                    allNames.AddRange(this.libraries.Select(l => l.Name));
                }

                //return unique names since a library can have the same name as an object
                return new HashSet<string>(allNames).ToArray();
            }
            finally
            {
                if (error.IsNullOrEmpty())
                    error = Logout();
                if (error.IsNullOrEmpty() == false)
                    throw new Exception(error);
            }
        }

        public IList<Description> GetLists(params string[] listNames)
        {
            if (listNames == null || listNames.Length == 0)
                return new List<Description>();

            string error = Login();
            if (error.IsNullOrEmpty() == false)
                throw new Exception(error);

            try
            {
                //get the object and list names first so that we can decide whether to call 'describeGlobal' with 'listName'.
                if (this.objectNames == null)
                {
                    this.objectNames = DescribeGlobal();
                }
                if (this.libraries == null && this.objectNames.Contains("ContentWorkspace"))
                {
                    this.libraries = GetLibraries();
                }

                List<Description> list = new List<Description>();
                SalesForceMetadata metadata = new SalesForceMetadata();
                var objects = this.objectNames.Intersect(listNames).ToList();
                if (objects.Count > 0)
                {
                    //Get metadata for the objects. Lists do not have actual tables that can be described via the describeSObjects method call! 
                    var metaObjects = GetMetaObjects(objects.ToArray());
                    list.AddRange(metaObjects.Select(m => GetDescription(m)));
                    metadata.Objects = metaObjects.ToArray();
                }

                var remaining = listNames.Except(objects);
                if (this.libraries != null)
                {
                    //There exists no separate table for each library. Create a description object for each library using the 'ContentDocument' and 'ContentVersion' metadata
                    MetaObject contentDocument = null;
                    MetaObject contentVersion = null;
                    foreach (var libraryName in remaining)
                    {
                        var listMetadata = this.libraries.SingleOrDefault(l => l.Name.Equals(libraryName));
                        if (listMetadata != null)
                        {
                            //Get the metadata for the ContentDocument object. 
                            if (contentDocument == null)
                            {
                                var objs = GetMetaObjects("ContentDocument", "ContentVersion");
                                contentDocument = objs[0];
                                contentVersion = objs[1];

                                //remove the 'Title' field from the 'ContentDocument'. The 'Title' field from ContentVersion should be used!
                                var titleField = contentDocument.Fields.Where(f => f.Name.Equals("Title")).First();
                                contentDocument.Fields.Remove(titleField);
                            }

                            listMetadata.Fields.AddRange(contentDocument.Fields);//Should these be new instances?
                            //Add all custom fields and other version specific fields which are important for the document
                            listMetadata.Fields.AddRange(contentVersion.Fields.Where(f => f.IsCustom || contentVersionFields.Contains(f.Name)));

                            list.Add(GetDescription(listMetadata));
                            metadata[libraryName] = listMetadata;
                        }
                    }
                }

                this.config.Configuration += metadata.ToString();

                return list;
            }
            finally
            {
                if (string.IsNullOrEmpty(error))
                    error = Logout();
                if (string.IsNullOrEmpty(error) == false)
                    throw new Exception(error);
            }
        }

        public object ForwardMap(IList<Description> metadata)
        {
            throw new NotImplementedException();
        }

        public DefaultMapCapabilities GetDefaultMapCapabilities()
        {
            if (this.mappingCapabilities == null)
            {
                this.mappingCapabilities = new SalesForceDefaultMapCapabilities();
            }

            return this.mappingCapabilities;
        }

        public IMetadataTransform GetMetadataTransform(DefaultMapCapabilities capabilities = null, bool isBase = false)
        {
            if (capabilities == null)
            {
                capabilities = this.GetDefaultMapCapabilities();
            }

            return new SalesForceMetadataTransform(capabilities);
        }

        private void ProcessConfig()
        {
            SalesForceConnectionStringBuilder builder = new SalesForceConnectionStringBuilder(this.config.ConnectionString);
            this.username = builder.User;
            this.password = builder.Password;
            if (builder.Token.IsNullOrEmpty() == false)
            {
                this.password = this.password + builder.Token;
            }
        }

        private string Login()
        {
            this.binding = new SforceService();
            this.binding.Timeout = 60000;

            LoginResult lr;
            try
            {
                lr = this.binding.login(username, password);
            }
            catch (SoapException e)
            {
                return string.Format("Saleforce login error {0} {1}", e.Code, e.Message);
            }

            if (lr.passwordExpired)
                return "Saleforce password has expired.";


            /** Once the client application has logged in successfully, it will use
                * the results of the login call to reset the endpoint of the service
                * to the virtual server instance that is servicing your organization
                */
            // Set returned service endpoint URL
            this.binding.Url = lr.serverUrl;

            /** The sample client application now has an instance of the SforceService
                * that is pointing to the correct endpoint. Next, the sample client
                * application sets a persistent SOAP header (to be included on all
                * subsequent calls that are made with SforceService) that contains the
                * valid sessionId for our login credentials. To do this, the sample
                * client application creates a new SessionHeader object and persist it to
                * the SforceService. Add the session ID returned from the login to the
                * session header
                */
            this.binding.SessionHeaderValue = new SessionHeader();
            this.binding.SessionHeaderValue.sessionId = lr.sessionId;

            return null;
        }

        private string Logout()
        {
            try
            {
                this.binding.logout();
                this.binding.Dispose();
                this.binding = null;
            }
            catch (SoapException e)
            {
                return string.Format("Saleforce logout error {0} {1}", e.Code, e.Message);
            }
            return null;
        }

        /// <summary>
        /// Gets the names of the types available to the logged-in user.
        /// Sample code available at - http://www.salesforce.com/us/developer/docs/api/Content/sforce_api_quickstart_steps.htm
        /// </summary>
        /// <returns></returns>
        private List<string> DescribeGlobal()
        {
            try
            {
                DescribeGlobalResult dgr = binding.describeGlobal();
                List<string> list = new List<string>();
                for (int i = 0; i < dgr.sobjects.Length; i++)
                {
                    list.Add(dgr.sobjects[i].name);
                }
                return list;
            }
            catch (SoapException e)
            {
                throw new Exception(string.Format("Saleforce description error {0} {1}", e.Code, e.Message), e);
            }
        }

        private List<MetaObject> GetLibraries()
        {
            var metadata = new SalesForceMetadata();
            var cwmetadata = new MetaObject { Name = "ContentWorkspace" };
            cwmetadata.Fields.Add(new MetaField { Name = "Name", ClrType = typeof(string).FullName });
            metadata["ContentWorkspace"] = cwmetadata;
            using (var context = typeof(SalesForceContext).CreateInstance<SalesForceContext>( this.binding.Url, this.binding.SessionHeaderValue.sessionId, metadata))
            {
                var query = new Telerik.OpenAccess.DataAdapter.SalesForce.Data.SalesForceQuery(cwmetadata);
                query.Fields.Add("Name");

                return context.ExecuteMethod<IEnumerable<SalesForceItem>>("ExecuteQuery", new[] { query }).Select(i => new MetaObject { Id = i.Id, Name = (string)i["Name"], IsContentLibrary = true }).ToList();
            }
        }

        /// <summary>
        /// Gets the metadata for the specifed SalesForce type.
        /// Sample code available at - http://www.salesforce.com/us/developer/docs/api/Content/sforce_api_quickstart_steps.htm
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        private Description GetDescription(MetaObject metaObject)
        {
            var desc = new Description();
            desc.Name = metaObject.Name;

            string objectTypeName = typeof(object).FullName;
            for (int i = 0; i < metaObject.Fields.Count; i++)
            {
                // Get the field 
                MetaField field = metaObject.Fields[i];

                var f = new FieldDescription();
                f.Name = field.Name;
                if (objectTypeName.Equals(field.ClrType))
                    continue;

                f.IsNullable = field.IsNullable;
                f.ClrType = field.ClrType;
                f.Length = field.Length;
                f.IsUnicode = f.ClrType == typeof(string).FullName;

                // Write the permissions of this field
                f.IsReadOnly = field.Createable == false && field.Updateable == false;//There is no other way to represent InsertOnly fields! (ex : PathOnClient is InsertOnly)

                f.IsBackendCalculated = field.IsBackendCalculated;
                f.CopyAfterPush = field.Createable == false && field.Updateable;//Not insertable but updateable fields. Is this the best way to represent this?
                f.IsIdentity = field.IsIdentity;
                if (f.IsIdentity)
                {
                    desc.IdentityField = f.Name;
                    desc.ProposedCorrelationFields.Add(f.Name);
                }
                f.IsConcurrencyControl = SystemModstamp.Equals(f.Name);

                var fType = ((Field)field.GetPropertyValue("Tag")).type;
                if (fType == fieldType.date)
                    f.ProposedConverter = new Telerik.OpenAccess.DataAdapter.Converter.ConverterConfiguration { TypeName = typeof(CustomDateTimeToDateConverter).AssemblyQualifiedName };
                else if(fType == fieldType.picklist)
                    f.ProposedConverter = new Telerik.OpenAccess.DataAdapter.Converter.ConverterConfiguration { 
                        TypeName = typeof(PicklistConverter).AssemblyQualifiedName, 
                        Configuration = string.Format(PicklistConverter.ConfigurationStringFormat, false)
                    };
                else if (fType == fieldType.multipicklist)
                    f.ProposedConverter = new Telerik.OpenAccess.DataAdapter.Converter.ConverterConfiguration
                    {
                        TypeName = typeof(PicklistConverter).AssemblyQualifiedName,
                        Configuration = string.Format(PicklistConverter.ConfigurationStringFormat, true)
                    };

                desc.Fields.Add(f);
                if (field.ExternalId)
                    desc.ProposedCorrelationFields.Add(f.Name);
            }

            return desc;
        }

        private List<MetaObject> GetMetaObjects(params string[] objectNames)
        {
            List<MetaObject> metaObjects = new List<MetaObject>();
            try
            {
                DescribeSObjectResult[] dsrArray = this.binding.describeSObjects(objectNames);
                for (int i = 0; i < dsrArray.Length; i++)
                {
                    var dsr = dsrArray[i];
                    MetaObject metaObject = new MetaObject();
                    metaObject.Name = dsr.name;

                    // Now, retrieve metadata for each field
                    for (int j = 0; j < dsr.fields.Length; j++)
                    {
                        // Get the field 
                        Field field = dsr.fields[j];

                        var metaField = new MetaField();
                        metaField.Name = field.name;
                        var t = MapType(field.type);
                        metaField.IsBackendCalculated = field.defaultedOnCreate || ContentSize.Equals(field.name);//the content size is backend calculated based on the VersionData contents!;
                        metaField.IsNullable = field.nillable;
                        metaField.ClrType = metaField.IsNullable ? Telerik.OpenAccess.DataAdapter.Util.Utils.GetNullableType(t).ToString() : t.ToString();
                        metaField.Length = field.type == fieldType.base64 ? -1 : field.length;
                        metaField.Createable = field.createable;
                        metaField.Updateable = field.updateable;
                        metaField.IsIdentity = field.type == fieldType.id;//will always be a single field called 'Id'
                        metaField.ExternalId = field.externalId;
                        metaField.IsCustom = field.custom;
                        metaField.ParentSFObject = dsr.name;
                        metaField.SetPropertyValue("Tag", field);
                        metaObject.Fields.Add(metaField);
                    }

                    metaObjects.Add(metaObject);
                }

                return metaObjects;
            }
            catch (SoapException e)
            {
                throw new Exception(string.Format("Saleforce metadata error {0} {1}", e.Code, e.Message), e);
            }
        }

        /// <summary>
        /// http://www.salesforce.com/us/developer/docs/api/index_Left.htm#CSHID=sforce_api_calls_describesobjects_describesobjectresult.htm|StartTopic=Content%2Fsforce_api_calls_describesobjects_describesobjectresult.htm
        /// </summary>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        private Type MapType(fieldType fieldType)
        {
            switch (fieldType)
            {
                case fieldType.@string:
                case fieldType.url:
                case fieldType.email:
                case fieldType.phone:
                case fieldType.encryptedstring:
                case fieldType.textarea:
                case fieldType.id:
                    return typeof(string);
                case fieldType.base64:
                    return typeof(byte[]);
                case fieldType.boolean:
                    return typeof(bool);
                case fieldType.currency:
                    return typeof(decimal);
                case fieldType.@int:
                    return typeof(int);
                case fieldType.@double:
                    return typeof(double);
                case fieldType.date:
                case fieldType.datetime:
                case fieldType.time:
                    return typeof(DateTime);
                case fieldType.picklist:
                case fieldType.multipicklist:
                    return typeof(string);
                case fieldType.combobox:
                case fieldType.reference:
                case fieldType.percent:
                case fieldType.datacategorygroupreference:
                case fieldType.location:
                case fieldType.anyType:
                default:
                    return typeof(object);
            }
        }
    }
}