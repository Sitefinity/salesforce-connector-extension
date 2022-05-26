Progress® Sitefinity® CMS SalesforceConnector Extension
====================================================

## Overview

The Sitefinity Salesforce Connector Extension extends the connector's existing build-in capabilities, providing support for picklist fields. The extension enables users to map Sitefinity choice fields with picklist fields in Salesforce and supports bidirectional sync for picklist and multi-picklist fields.

## Prerequisites

To use the extension, you need to build it from the source code. Make sure that your development system meets the following minimal requirements:

* A valid Sitefinity CMS license.
* Sitefinity CMS 14.1 or later.
* Your setup must comply with the minimum system requirements.
For more information, see the [System requirements](https://www.progress.com/documentation/sitefinity-cms/system-requirements) for the Sitefinity CMS version you are using.
* Visual Studio 2015 or later.

## Building the solution

This readme file assumes that you are using Visual Studio 2019. The older versions of Visual Studio may have small changes in the described UI elements but the process is very similar.

To use `Sitefinity.SalesforceConnector.Extension` with your Sitefinity CMS site perform the following:

1. Download this repository it into your Sitefinity CMS solution on your local drive
2. In Visual Studio open your Sitefinity solution, for example `SitefinityWebApp.sln`.
3. Add the downloaded `Sitefinity.SalesforceConnector.Extension.csproj` to your Visual Studio solution. Perform the following:
   1. Navigate to *File » Add » Existing project...*
   2. Browse to `c:\work\ salesforce-connector-extension`. The exact path depends on where you have cloned the repository
   3. Browse to Sitefinity.SalesforceConnector.Extension
   4. Select `Sitefinity.SalesforceConnector.Extension.csproj` and click *Open*.
4. In your main Sitefinity project, for example `SitefinityWebApp`, add a project reference to the `Sitefinity.SalesforceConnector.Extension` project. Perform the following:
   1. Select your main Sitefinity project, for example `SitefinityWebApp` in the *Solution Explorer*
   2. In the main menu, Navigate to Project » Add Reference... A dialog opens.
   3. Navigate to *Project » Solution*.
   4. Check the `Sitefinity.SalesforceConnector.Extension` in the list on the right.
   5. Click *OK*
5. Build your Visual Studio solution.

*Note:* The Sitefinity Salesforce Connector Extension project depends on specific NuGet packages for Sitefinity CMS. When you include the Sitefinity Salesforce Connector Extension project in your solution that depends on a newer Sitefinity version, you must also update the ```Telerik.Sitefinity.Core```, ```Telerik.Sitefinity.SalesForceConnector```, ```Telerik.OpenAccess.Octopus```  dependencies of the Sitefinity Salesforce Connector Extension project to match your Sitefinity version.

## Using the Salesforce Connector Extension in your project

After you build the `Sitefinity.SalesforceConnector.Extension` project, you can sync your choice fields to picklist fields in Salesforce. To do this, perform the following:

* Start your Sitefinity CMS solution
* In the browser, navigate to your Sitefinity CMS backend
* Navigate to Administration » Connector for SalesForce and add a new Salesforce account or use an existing one
* Add new sync or edit existing one
* Open Mappings dialog
* The picklist and multi picklist fields would be listed there
* Map the fields that you need to choice fields in Sitefinity
* Click *Save* to save your changes.

## How does it work

The code in the `Installer` class registers a custom `SalesForceMetadataAdapter` class replacing the built-in one. There are a couple of changes to the default logic. First, in the `MapType` method, picklist and multipicklist fields are mapped to string. It is important to be mapped to a type different from the Object because the object fields are skipped during the sync process. The second change is in the `GetDescription` method, where custom `PicklistConverter` is registered for both field types. It converts the values to suitable formats for both systems, Sitefinity and Salesforce. Default `DateTimeConverter` is also replaced to resolve an issue when parsing date strings.

*Note:* The structure/metadata(defined choice options) between the picklist fields in Salesforce and choice fields in Sitefinity is not synced. Therefore, they should be manually created/updated, so the fields in Sitefinity and Salesforce are in sync (with the same predefined options).
