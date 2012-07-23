namespace MvcApi.Test
{
    using System.Collections.Generic;
    using System.Web;
    using Moq;

    public static class HttpContextHelpers
    {
        public static Mock<HttpContextBase> GetMockHttpContext()
        {
            Mock<HttpContextBase> mockContext = new Mock<HttpContextBase>();
            mockContext.Setup(m => m.Items).Returns(new Dictionary<object, object>());
            return mockContext;
        }
    }
}
