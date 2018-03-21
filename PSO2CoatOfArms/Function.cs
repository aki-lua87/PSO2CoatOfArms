using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using HtmlAgilityPack;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PSO2CoatOfArms
{
    public class Function
    {
        const string projectName = "PSO2CoatOfArms";
        const string pso2Url = "http://pso2.jp/players/news/i_hget/";

        private static readonly AmazonDynamoDBClient Client = new AmazonDynamoDBClient(RegionEndpoint.APNortheast1);

        public bool FunctionHandler(ILambdaContext context)
        {
            try
            {
                var dbContext = new DynamoDBContext(Client);

                var dbContents = new TableValue();
                dbContents.ProjectName = projectName;

                var html = (new HttpClient()).GetStringAsync(pso2Url).Result;

                var doc = new HtmlDocument();
                doc.OptionAutoCloseOnEnd = false;
                doc.OptionCheckSyntax = false;
                doc.OptionFixNestedTags = true;

                //サイト全体の読み込み
                doc.LoadHtml(html);

                var events = doc.DocumentNode.SelectNodes($"//th[@class='sub']");

                dbContents.StringList = new List<string>();
                foreach (var eventNode in events)
                {
                    context.Logger.Log(eventNode.InnerHtml);
                    dbContents.StringList.Add(eventNode.InnerHtml);
                }
                dbContents.UpdateTime = DateTime.UtcNow.ToString();

                var insertTask = dbContext.SaveAsync(dbContents);
                insertTask.Wait();
            }
            catch (Exception e)
            {
                return false;
            }
            return true;

        }
    }

    [DynamoDBTable("common")]
    public class TableValue
    {
        [DynamoDBHashKey]
        public string ProjectName { get; set; }

        [DynamoDBProperty("StringList")]
        public List<string> StringList { get; set; }


        [DynamoDBProperty("UpdateTime")]
        public string UpdateTime { get; set; }
    }
}
