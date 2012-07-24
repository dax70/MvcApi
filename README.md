MVC API
-------

MVC API is a Web Api framework built on top ASP.NET MVC.

It is nearly identical to ASP.NET Web API, infact it strives to be so. 

Features
--------
### Queries
Return **IQueryable** from your action and use [OData](http://www.odata.org/) operators to further filter your queries.

**Ex:** 
http://localhost:port/products?$orderby=price 

query.OrderBy(p => p.Price); // Equivalent C#

http://localhost/products?$filter=price gt 20

query.Where(p => p.Price > 20); // Equivalent C#

Whether these queries were produced via javascript, or found in an anchor tag, it's a URI to the server, which gets translated into the proper Linq Where, Orderby, GroupBy, etc. And of course these can be combined, to compose complex queries.
This is similar to the Web API OData filters that were pulled out of the RC.

### Actions
By default Actions get mapped to the HttpVerb, which frees yourself from always specifying actions in routes.
Ex: 
* /customers will resolve to the IQueryable 
* /customers/1 will resolve to the Single.

### Content Negotiation
Liberate yourself to return *Objects* instead of **ActionResult** thru the magic of content negotiation. Take a look at the example below or the samples folder.
If however your use needs to explicitly return an ActionResult, your free to do so, still works. The current Content Negotiation supports: Html, Json and Xml.
Content negotiation works by letting the client request the format. The browser's default request format is text/html, but an ajax client could request application/json. 
In the sample below both work just fine, since it's returning objects, the framework figures out how to format (serialize) the response.

Basic Usage
-----------

    // ApiController inherits from Controller, but overrides certain things mainly the ControllerDispatcher.
    public ProductsController: ApiController
    {
        var db ... // Some EF boilerplate code.
        public IQueryable<Product> Get()
        {
            return dbContext.Products; 
        }
        
        public Product Get(int id)
        {
            return dbProducts.Where(p => p.Id == id).Single();	
        }
        
        [HttpPost] / * Optional since naming convention assumes post */
        public Product Post(Product product)
        {
            db.Products.AddObject(product);	
            db.SaveChanges();
            return product; // Echo object with populated Id.
        }
        
        [HttpPut] / * Optional */
        public Product Put(Product product)
        {
            db.Products.Attach(product);
            db.ObjectStateManager.ChangeObjectState(product, EntityState.Modified);
            db.SaveChanges();
        }
        
        [HttpDelete / * Optional */
        public int  Delete(int id) { // etc }
    }
Views
-----
The views are matched based on the Action name just like any MVC controller, in fact it's all MVC under the covers just extending the core framework at existing extensibility points so that it aligns almost 100% with Web API functionality, but reuses existing investments/code that is already on MVC.

Why another API framework for ASP.NET?
-----------------------------------------

The major difference is that existing projects have Controllers, Views, ActionFilters and other dependencies on MVC. 
Rewritting these or migrating them to the similar Web API is not feasible for many projects.

Additionally, Web API has the concept of MediaTypeFormatters, but does not have a proper HTML one. 
If you need Razor views that work with models, without making everything go thru an ajax call to render UI then MVC API might be for you.
