namespace MvcApi.Test
{
    #region Using Directives
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Web.Routing;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.TestUtil;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.Net;
    #endregion

    [TestClass]
    public class ApiControllerActionSelectorTest
    {
        [TestMethod]
        public void ConventionGetQueryable()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            // Act
            ApiControllerActionSelector selector = new ApiControllerActionSelector();

            ApiActionDescriptor actionDescriptor = (ApiActionDescriptor)selector.SelectAction(controllerContext);

            // Assert
            Assert.AreEqual("get", actionDescriptor.ActionName, true);
            Assert.AreEqual(0, actionDescriptor.GetParameters().Length);
            Assert.AreEqual(typeof(IQueryable<UserModel>), actionDescriptor.ReturnType);
        }

        [TestMethod]
        public void ConventionGetWithId()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext(routeValues: new { id = 3 });

            // Act
            ApiControllerActionSelector selector = new ApiControllerActionSelector();

            ApiActionDescriptor actionDescriptor = (ApiActionDescriptor)selector.SelectAction(controllerContext);

            // Assert
            Assert.AreEqual("get", actionDescriptor.ActionName, true);
            Assert.AreEqual(1, actionDescriptor.GetParameters().Length);
            Assert.AreEqual(typeof(UserModel), actionDescriptor.ReturnType);
        }

        [TestMethod]
        public void ConventionPost()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext("POST");

            // Act
            ApiControllerActionSelector selector = new ApiControllerActionSelector();

            ApiActionDescriptor actionDescriptor = (ApiActionDescriptor)selector.SelectAction(controllerContext);

            // Assert
            Assert.AreEqual("post", actionDescriptor.ActionName, true);
            Assert.AreEqual(1, actionDescriptor.GetParameters().Length);
            Assert.AreEqual(typeof(UserModel), actionDescriptor.GetParameters()[0].ParameterType);
        }

        [TestMethod]
        public void ConventionOnlyOnPost()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext("GET", routeValues: new { action = "post" });
            ApiControllerActionSelector selector = new ApiControllerActionSelector();
            
            // Act
            ExceptionHelper.ExpectHttpException(
                delegate() { selector.SelectAction(controllerContext); },
                "The requested resource does not support http method 'GET'.",
                405 // HttpStatusCode: MethodNotAllowed
            );
        }

        [TestMethod]
        public void ConventionPut()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext("PUT", routeValues: new { id = 3 });

            // Act
            ApiControllerActionSelector selector = new ApiControllerActionSelector();

            ApiActionDescriptor actionDescriptor = (ApiActionDescriptor)selector.SelectAction(controllerContext);

            // Assert
            Assert.AreEqual("put", actionDescriptor.ActionName, true);
            Assert.AreEqual(2, actionDescriptor.GetParameters().Length);
            Assert.AreEqual(typeof(int), actionDescriptor.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(UserModel), actionDescriptor.GetParameters()[1].ParameterType);
        }

        [TestMethod]
        public void ConventionPutOverrideInHeader()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext("POST", routeValues: new { id = 3 }, requestHeaders: new { X_Http_Method_Override = "PUT" });

            // Act
            ApiControllerActionSelector selector = new ApiControllerActionSelector();

            ApiActionDescriptor actionDescriptor = (ApiActionDescriptor)selector.SelectAction(controllerContext);

            // Assert
            Assert.AreEqual("put", actionDescriptor.ActionName, true);
            Assert.AreEqual(2, actionDescriptor.GetParameters().Length);
            Assert.AreEqual(typeof(int), actionDescriptor.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(UserModel), actionDescriptor.GetParameters()[1].ParameterType);
        }

        [TestMethod]
        public void ConventionPutOverrideInForm()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext("POST", routeValues: new { id = 3 }, formValues: new { X_Http_Method_Override = "PUT" });

            // Act
            ApiControllerActionSelector selector = new ApiControllerActionSelector();

            ApiActionDescriptor actionDescriptor = (ApiActionDescriptor)selector.SelectAction(controllerContext);

            // Assert
            Assert.AreEqual("put", actionDescriptor.ActionName, true);
            Assert.AreEqual(2, actionDescriptor.GetParameters().Length);
            Assert.AreEqual(typeof(int), actionDescriptor.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(UserModel), actionDescriptor.GetParameters()[1].ParameterType);
        }

        #region Factory Helpers

        private static ControllerContext GetControllerContext(string httpVerb = "GET", object routeValues = null, object requestHeaders = null, object formValues = null)
        {
            RouteData routeData = new RouteData();
            routeData.Values.Add("controller", "methodlocator");

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(routeValues))
            {
                routeData.Values.Add(property.Name, property.GetValue(routeValues));
            }


            var httpContextBase = GetHttpContextWithHttpVerb(httpVerb, requestHeaders: UnitTestHelper.ToNameValue(requestHeaders), form: UnitTestHelper.ToNameValue(formValues));
            MethodLocatorController controller = new MethodLocatorController();
            ControllerContext controllerContext = new ControllerContext(httpContextBase, routeData, controller);
            return controllerContext;
        }

        private static HttpContextBase GetHttpContextWithHttpVerb(string httpVerb, NameValueCollection requestHeaders = null, NameValueCollection form = null, NameValueCollection queryString = null)
        {
            Mock<HttpContextBase> mockHttpContext = HttpContextHelpers.GetMockHttpContext();
            mockHttpContext.Setup(c => c.Request.HttpMethod).Returns(httpVerb);
            // Action Selector checks for query strings.
            mockHttpContext.Setup(c => c.Request.QueryString).Returns(queryString ?? new NameValueCollection());
            // Used in HttpPosts to check override for browser unsupported verbs (Put, Delete, Head, etc).
            mockHttpContext.Setup(c => c.Request.Headers).Returns(requestHeaders ?? new NameValueCollection());
            mockHttpContext.Setup(c => c.Request.Form).Returns(form ?? new NameValueCollection());
            return mockHttpContext.Object;
        }

        private static Mock<HttpContextBase> GetMockHttpContextWithHttpVerb(string httpVerb, NameValueCollection requestHeaders = null, NameValueCollection form = null, NameValueCollection queryString = null)
        {
            Mock<HttpContextBase> mockHttpContext = HttpContextHelpers.GetMockHttpContext();
            mockHttpContext.Setup(c => c.Request.HttpMethod).Returns(httpVerb);
            // Action Selector checks for query strings.
            mockHttpContext.Setup(c => c.Request.QueryString).Returns(queryString ?? new NameValueCollection());
            // Used in HttpPosts to check override for browser unsupported verbs (Put, Delete, Head, etc).
            mockHttpContext.Setup(c => c.Request.Headers).Returns(requestHeaders ?? new NameValueCollection());
            mockHttpContext.Setup(c => c.Request.Form).Returns(form ?? new NameValueCollection());
            return mockHttpContext;
        }
        #endregion

        #region Sample Controller & Data

        private class MethodLocatorController : ApiController
        {
            // GET /convention
            public IQueryable<UserModel> Get()
            {
                return UserModel.Users.AsQueryable();
            }

            // GET /convention/5
            public UserModel Get(int id)
            {
                return UserModel.Users.First(u => u.Id == id);
            }

            // GET /convention/new
            public UserModel New()
            {
                // seed view so that validation works, not needed on ajax views.
                return new UserModel();
            }

            // POST /convention
            public UserModel Post(UserModel user)
            {
                // echo entity with Id populated and can create location.
                user.Id = UserModel.Users.Length; // Simulate Id gen.
                return user;
            }

            // PUT /convention/5
            public UserModel Put(int id, UserModel user)
            {
                return user;
            }

            // DELETE /convention/5
            public void Delete(int id)
            {
            }
        }

        #endregion
    }
}
