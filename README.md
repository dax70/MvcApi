Droopy
======

Web Api clone for MVC


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

The views are matched based on naming just like any MVC controller, in fact it's all MVC under the covers just extending the core framework at existing extensibility points so that it aligns almost 100% with Web API functionality, but reuses existing investments/code that is already on MVC.

A couple things (I really like) of interest.

The IQueryable can be filtered by the client using OData opertators, such as :
http://localhost:port/products?$orderby=price or 
http://localhost/products?$filter=price gt 20

It doesnt matter how these are produced, ie javascript of anchor tag, it's a URI to the server, which gets translated into the proper Linq Orderby, Where, GroupBy, etc. And of course these can be combined, composing complex queries.

The other piece is the content negotiation, not returning an explicit ActionResult such as View, or JsonResult, etc, let's the client request the format. So the browser by default requests text/html, but an ajax client could request application/json. In the sample above both work just fine, since it's returning objects, the framework figures out how to format (serialize) the response.
