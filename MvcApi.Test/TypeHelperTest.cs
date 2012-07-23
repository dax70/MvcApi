using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MvcApi.Test
{
    [TestClass]
    public class TypeHelperTest
    {
        [TestMethod]
        public void IsSimpleType_String()
        {
            // Arrange
            Type type = typeof(string);
            // Act
            bool actual = TypeHelper.IsSimpleType(type);
            // Assert
            Assert.IsTrue(actual);
        }
    }
}
