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

        [HttpGet]
        [Route("[action]/{year}/{month}")]
        //[Authorize]
        public IActionResult SumSalePerMonth(int year, int month) {
            try {
                int totalDay = DateTime.DaysInMonth(year, month);
                List<object> list = new List<object>();

                for (int i = 1; i <= totalDay; i++) {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT SUM(qty * price) AS totalSum 
                        FROM tb_bill_sale_detail
                        LEFT JOIN tb_bill_sale ON tb_bill_sale.id = tb_bill_sale_detail.bill_sale_id
                        WHERE
                            pay_at IS NOT NULL
                            AND (
                                EXTRACT(YEAR FROM pay_at) = @year
                                AND EXTRACT(MONTH FROM pay_at) = @month
                                AND EXTRACT(DAY FROM pay_at) = @day
                            )
                    ";
                    cmd.Parameters.AddWithValue("year", year);
                    cmd.Parameters.AddWithValue("month", month);
                    cmd.Parameters.AddWithValue("day", i);

                    object obj = cmd.ExecuteScalar()!;

                        list.Add(new {
                            totalSum = obj.GetType().ToString() == "System.DBNull" ? 0 : obj,
                            day = i
                        });
                }

                return Ok(new { results = list });
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
        }
    }
}
