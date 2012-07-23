using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcApi.Formatting;
using System.IO;

namespace MvcApi.Test
{
    [TestClass]
    public class JsonMediaTypeFormatterTest
    {
        [TestMethod]
        public void WriteQueryable()
        {
            // Arrange
            //var models = UserModel.Users.AsQueryable();
            var models = new UserModel[] { new UserModel { Id = 1, Name = "Joe" } }.AsQueryable();
            var formatter = new JsonMediaTypeFormatter();
            var converter = new ObjectConverter<UserModel>();
            converter.Mapper = (u) => new { Key = u.Id, UserName = u.Name };
            JsonContractFormatter.AddConverter(typeof(UserModel), converter);
            string actual = string.Empty;

            // Act
            //formatter.ExecuteFormat(models.GetType(), models, null);
            //formatter.OnWrite(models.GetType(), models, null);
            using (StringWriter writer = new StringWriter())
            {
                formatter.WriteTo(models.GetType(), models, writer);
                actual = writer.ToString();
            }
            // Remember to Add 'Items' on naked Queryables 
            Assert.AreEqual("{\"$id\":\"1\",\"Items\":[{\"$id\":\"2\",\"Id\":1,\"Name\":\"Joe\",\"Email\":null}]}", actual);
        }
    }
}
