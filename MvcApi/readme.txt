Welcome to using MvcApi.

Views 
------------------------------------------------
To enable default view resolution rules add the following code somewhere in the application
start up cycle such as the Globla.asax 'Application_Start'.

/* Enable default REST view engine rules */
GlobalConfiguration.Configuration.EnableDefaultViews(ViewLocations.Locations);

OData
------------------------------------------------
If you want OData support on your Queryable, then add the following line of code to your filters.

/* Enables Queryable (incomplete) OData support */
GlobalFilters.Filters.Add(new QueryableFilterProvider());

If you prefer you can simply add this QueryFilterAttribute on the specific action methods
for more granular control.

For more check out the project's website https://github.com/dax70/MvcApi
The source includes and sample application.
