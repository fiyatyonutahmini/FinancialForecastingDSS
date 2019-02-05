using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinancialForecastingDSS.indicators
{
    class IndicatorService
    {
        public static int SMA = 0;
        public static int WMA = 1;
        public static int EMA = 2;
        public static int MACD = 3;
        public static int RSI = 4;
        public static int WilliamsR = 5;
        public static int Stochastics = 6;
        public static int[] indicators = { SMA, WMA, EMA, MACD, RSI, WilliamsR, Stochastics };

        private static BsonDocument sort = new BsonDocument("Tarih", -1);

        private IndicatorService() { }

        public static List<BsonDocument> GetCodeList()
        {
            var collection = MongoDBService.GetService().GetCollection();

            PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[]
            {
                new BsonDocument("$group", new BsonDocument("_id", "$Kod"))
            };

            List<BsonDocument> codeList = null;
            try
            {
                codeList = collection.Aggregate(pipeline).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return codeList;
        }

        public static DateTime LastDate(string code)
        {
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("Kod", code)),
                new BsonDocument() { { "$group",
                        new BsonDocument() {
                            { "_id", new BsonDocument() },
                            { "maxDate", new BsonDocument("$max", "$Tarih") },
                        } } },
                new BsonDocument("$project", new BsonDocument("_id", 0))
            };

            try
            {
                var date = MongoDBService.GetService().GetCollection().Aggregate(pipeline).ToList().ElementAt(0);
                return date.GetElement("maxDate").Value.ToLocalTime();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int DataCount(string code, DateTime targetDate)
        {
            return MongoDBService.GetService().Count(new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", new BsonDateTime(targetDate)) } });
        }

        public static List<BsonDocument> GetData(string code, DateTime targetDate)
        {
            BsonDocument filter = new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", targetDate) } };
            return MongoDBService.GetService().FindManySort(filter, sort);
        }

        public static List<BsonDocument> GetData(string code, DateTime targetDate, int limit)
        {
            BsonDocument filter = new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", targetDate) } };
            return MongoDBService.GetService().FindManySortLimit(filter, sort, limit);
        }

        public static List<BsonDocument> GetData(string code, DateTime targetDate, string projectionField)
        {
            BsonDocument filter = new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", targetDate) } };
            BsonDocument projection = new BsonDocument { { "_id", 0 }, { projectionField, 1 } };
            return MongoDBService.GetService().FindManySortProject(filter, sort, projection);
        }

        public static List<BsonDocument> GetData(string code, DateTime targetDate, string[] projectionFields)
        {
            BsonDocument filter = new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", targetDate) } };
            BsonDocument projection = new BsonDocument("_id", 0);
            foreach (string field in projectionFields) projection.Add(new BsonElement(field, 1));
            return MongoDBService.GetService().FindManySortProject(filter, sort, projection);
        }

        public static List<BsonDocument> GetData(string code, DateTime targetDate, string projectionField, int limit)
        {
            BsonDocument filter = new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", targetDate) } };
            BsonDocument projection = new BsonDocument { { "_id", 0 }, { projectionField, 1 } };
            return MongoDBService.GetService().FindManySortProjectLimit(filter, sort, projection, limit);
        }

        public static List<BsonDocument> GetData(string code, DateTime targetDate, string[] projectionFields, int limit)
        {
            BsonDocument filter = new BsonDocument { { "Kod", code }, { "Tarih", new BsonDocument("$lte", targetDate) } };
            BsonDocument projection = new BsonDocument("_id", 0);
            foreach (string field in projectionFields) projection.Add(new BsonElement(field, 1));
            return MongoDBService.GetService().FindManySortProjectLimit(filter, sort, projection, limit);
        }
    }
}
