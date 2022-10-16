using RatesWebService.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Data;

namespace RatesWebService.Controllers;

[ApiController]
[Route("currency")]
public class RateController : ControllerBase
{
    private IConfiguration Configuration;
    private string NBDateFormat = "dd.MM.yyyy";
    
    //XML возвращенный АПИ Нацбанка
    XDocument? retrievedXml;
    //кол-во добавленных в БД строк
    int rowsAffectedCount;
    string sqlConnString;
    
    public RateController(IConfiguration _configuration)
    {
        Configuration = _configuration;
        sqlConnString = this.Configuration.GetConnectionString("testDB");
    }


    //Первый АПИ метод из задачи
    [HttpGet("save/{date}")]
    [Produces("application/json")]
    public ActionResult<Rate> GetRatesAndUpdateDB(String date)
    {
        rowsAffectedCount = 0;
        DateTime formattedDate = DateTime.ParseExact(date, NBDateFormat, CultureInfo.CurrentCulture);
        //Вызов метода для обращения в АПИ Нацбанка. Ответ сохраняем в XDocument retrievedXml
        Task<string> responseBody = GetRatesByDateFromNB(formattedDate);
        retrievedXml = XDocument.Parse(responseBody.Result);

        //заполняем локальную БД данными из retrievedXml с помощью БД-процедуры spUpdateRates
        using (SqlConnection sqlConn = new SqlConnection(sqlConnString))
        using (SqlCommand command = new SqlCommand("dbo.spUpdateRates", sqlConn) { 
                                CommandType = CommandType.StoredProcedure })
        {
            command.Parameters.Add(new SqlParameter("@TITLE", SqlDbType.NVarChar, 60));
            command.Parameters.Add(new SqlParameter("@CODE", SqlDbType.NVarChar, 3));
            command.Parameters.Add(new SqlParameter("@VALUE", SqlDbType.Float, 18));
            command.Parameters.Add(new SqlParameter("@DATE", SqlDbType.Date));

            command.Connection.Open();

            string currencyTitle;
            string currencyCode;
            decimal currencyValue;

            //по условию задачи, в БД должна уходить дата именно из XML
            date = retrievedXml.Element("rates")!.Element("date")!.Value;
            DateTime dateFromXml = DateTime.ParseExact(date, NBDateFormat, CultureInfo.CurrentCulture);
            
            foreach (XElement xe in retrievedXml.Descendants("item"))
            {
                currencyTitle = xe.Element("fullname")!.Value;
                currencyCode = xe.Element("title")!.Value;
                currencyValue = decimal.Parse(xe.Element("description")!.Value, CultureInfo.InvariantCulture.NumberFormat);

                command.Parameters["@TITLE"].Value = currencyTitle;
                command.Parameters["@CODE"].Value = currencyCode;
                command.Parameters["@VALUE"].Value = currencyValue;
                command.Parameters["@DATE"].Value = dateFromXml;

                if (command.ExecuteNonQuery() == 1)
                    rowsAffectedCount++;
            }
        }

        return new JsonResult(new {count=rowsAffectedCount});

    }

    //Второй АПИ метод из задачи
    [HttpGet("{date}/{*code}")]
    [Produces("application/json")]
    public ActionResult<Rate> GetTable(String date, String? code = null)
    {
        DateTime formattedDate = DateTime.ParseExact(date, NBDateFormat, CultureInfo.CurrentCulture);

        using (SqlConnection sqlConn = new SqlConnection(sqlConnString))
        using (SqlCommand command = new SqlCommand("dbo.spGetRates", sqlConn) { 
                                CommandType = CommandType.StoredProcedure })
        {
            //Параметр @CODE всегда отправляется и на стороне сервера проверяется на НУЛЛ-значение
            command.Parameters.AddWithValue("@A_DATE", formattedDate);
            command.Parameters.AddWithValue("@CODE", code);

            command.Connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            var serialisedResult = Serialize(reader);
            
            return new JsonResult(serialisedResult);
        }
    }


    //обращение к АПИ Нацбанка
    private async Task<String> GetRatesByDateFromNB(DateTime date)
    {
        HttpClient httpClient = new HttpClient();
        string responseString = "";
        try
        {
            string uri = "https://nationalbank.kz/rss/get_rates.cfm?fdate="
                                                    + date.ToString(NBDateFormat);
            Task<string> datatask = httpClient.GetStringAsync(uri);
            
            responseString = await datatask;

        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");	
            Console.WriteLine("Message :{0} ",e.Message);
        }
        
        return responseString;
    }
    






    // Следующие два метода взяты из интернета: https://stackoverflow.com/a/16166658
    // Хотел минимизировать кол-во добавленных библиотек и использовал этот код для сериализации джейсон.
    private IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
    {
        var results = new List<Dictionary<string, object>>();
        var cols = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++) 
            cols.Add(reader.GetName(i));

        while (reader.Read()) 
            results.Add(SerializeRow(cols, reader));

        return results;
    }
    private Dictionary<string, object> SerializeRow(IEnumerable<string> cols, 
                                                    SqlDataReader reader) {
        var result = new Dictionary<string, object>();
        foreach (var col in cols) 
            result.Add(col, reader[col]);
        return result;
    }


}
