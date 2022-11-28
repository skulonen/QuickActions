# QuickActions

A productivity-boosting add-in for ArcGIS Pro.

Replaces the context menu of layers and tables in the table of context with a popup window that provides quick access to common workflows.

## Popup tabs

### Options

![Options](docs/tabs-options.png)

Change visibility, selectability, editability and other attributes of the layer that you would normally change on the different tabs in the table of contents.

### Source

![Source](docs/tabs-source.png)

Check the data source of the layer.

This one is still very much a work in process. I'd like to embed the layer source property page here, but that doesn't appear to be possible without some heavy reflection to access internal members. Currently, it displays the data connection XML document in a tree view, but that isn't nearly as convenient as the property page.

### Find

![Find](docs/tabs-find-1.png)

![Find](docs/tabs-find-2.png)

Search for features. From the search result list, you can easily select the result features, zoom the map to them, or view their attributes.

### Filter

![Filter](docs/tabs-filter.png)

Change the definition query of the layer.

### Symbology

![Symbology](docs/tabs-symbology.png)

Quickly symbolize the layer by using a classification field. You can also copy and paste symbols between layers.

### Create

![Create](docs/tabs-create.png)

Select a template and start drawing a new feature. If the layer has no templates, the add-in can automatically generate a basic one for you.

### Fields

![Fields](docs/tabs-fields-1.png)

![Fields](docs/tabs-fields-2.png)

View a list of the layer's fields. You can also inspect the value lists of fields that use coded value domains.

### Service

![Service](docs/tabs-service.png)

Contains links to the layer's source service in REST, Server Manager and Server Admin. You can also stop and start the service, provided the you have the required permissions.

### Definition

![Definition](docs/tabs-definition.png)

View and edit the layer's CIM definition directly. Mostly useful for developers.
