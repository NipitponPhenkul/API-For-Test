using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TestApi.Models;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        [Authorize]
        public IActionResult BillSale(ReportBillSaleModel reportBillSaleModel) {
            try {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT * FROM tb_bill_sale
                    WHERE pay_at BETWEEN @from AND @to
                    ORDER BY id DESC
                ";
                cmd.Parameters.AddWithValue("from", Convert.ToDateTime(reportBillSaleModel.FromDate + " 00:00"));
                cmd.Parameters.AddWithValue("to", Convert.ToDateTime(reportBillSaleModel.ToDate + " 23:59"));

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                List<object> list = new List<object>();

                while (reader.Read()) {
                    list.Add(new {
                        id = Convert.ToInt32(reader["id"]),
                        pay_at = Convert.ToDateTime(reader["pay_at"])
                    });
                }
                return Ok(new { results = list });
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("[action]/{billSaleId}")]
        [Authorize]
        public IActionResult BillSaleDetail(int billSaleId) {
            try {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        tb_bill_sale_detail.qty,
                        tb_bill_sale_detail.price,
                        tb_book.isbn,
                        tb_book.name
                    FROM tb_bill_sale_detail
                    LEFT JOIN tb_book ON tb_book.id = tb_bill_sale_detail.book_id
                    WHERE bill_sale_id = @billSaleId
                    ORDER by tb_bill_sale_detail.id DESC
                ";
                cmd.Parameters.AddWithValue("billSaleId", billSaleId);

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                List<object> list = new List<object>();

                while (reader.Read()) {
                    list.Add(new {
                        qty = Convert.ToInt32(reader["qty"]),
                        price = Convert.ToInt32(reader["price"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString()
                    });
                }
                return Ok(new { results = list, message = "success" });
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
        }
    }
}
