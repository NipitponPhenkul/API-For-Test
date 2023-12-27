﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TestApi.Models;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        [HttpGet]
        [Route("[action]")]
        [Authorize]
        public IActionResult List()
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection(); //เชื่อมฐานข้อมูล
                using NpgsqlCommand cmd = conn.CreateCommand(); //สร้าง Command เพื่อใช้ป้อนคำสั่ง SQL
                cmd.CommandText = "SELECT * FROM tb_book";

                using NpgsqlDataReader reader = cmd.ExecuteReader(); //สั่งให้อ่านคำสั่ง SQL เก็บไว้ใน reader
                List<object> list = new List<object>(); //สร้าง List ขึ้นมา
                while (reader.Read())
                {
                    list.Add(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString(),
                        price = Convert.ToInt32(reader["price"])
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("[action]/{id}")]
        [Authorize]
        public IActionResult Info(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM tb_book WHERE id = @id";
                cmd.Parameters.AddWithValue("id", id);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return Ok(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString(),
                        price = Convert.ToInt32(reader["price"])
                    });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new
                    {
                        message = "not found id"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize]
        public IActionResult Edit(BookModel bookModel)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @" 
                    UPDATE tb_book SET
                        isbn = @isbn,
                        name = @name,
                        price = @price
                    WHERE id = @id
                ";
                //@"" <- ความหมายคือถ้าจะเขียน string แบบหลาย ๆ บรรทัดต้องใช้แบบนี้
                cmd.Parameters.AddWithValue("isbn", bookModel.isbn!);
                cmd.Parameters.AddWithValue("name", bookModel.name!);
                cmd.Parameters.AddWithValue("price", bookModel.price);
                cmd.Parameters.AddWithValue("id", bookModel.id);
                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { message = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new
                    {
                        message = "update error"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut]
        [Route("[action]")]
        [Authorize]
        public IActionResult Create(BookModel bookModel)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO tb_book(isbn, name, price) VALUES(@isbn, @name, @price)";
                cmd.Parameters.AddWithValue("isbn", bookModel.isbn!);
                cmd.Parameters.AddWithValue("name", bookModel.name!);
                cmd.Parameters.AddWithValue("price", bookModel.price!);

                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { message = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new
                    {
                        message = "insert error"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete]
        [Route("[action]/{id}")]
        [Authorize]
        public IActionResult Remove(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM tb_book WHERE id = @id";
                cmd.Parameters.AddWithValue("id", id);

                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { message = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new
                    {
                        message = "delete error"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize]
        public IActionResult Sale(string barcode) {
           try {
                // 1 find product
                int id = 0;
                int price = 0;

                {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id, price FROM tb_book WHERE isbn = @barcode";
                    cmd.Parameters.AddWithValue("barcode", barcode);

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read()) {
                        id = Convert.ToInt32(reader["id"]);
                        price = Convert.ToInt32(reader["price"]);
                    } else {
                        return Ok(new { message = "not found" });
                    }
                }
                // 2 create bill sale
                int billSaleId = 0;
                {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id FROM tb_bill_sale WHERE pay_at IS NULL"; //เลือก id จาก tb_bill_sale เงื่อนไข pay_at ต้องเป็นค่า null

                    using NpgsqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read()) {
                        billSaleId = Convert.ToInt32(reader["id"]); // ถ้าอ่านค่าได้ให้แปลง id เป็น int
                    }
                }

                if (billSaleId == 0) { 
                    {
                        using NpgsqlConnection conn = new Connect().GetConnection();
                        using NpgsqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO tb_bill_sale(created_at) VALUES(NOW()) RETURNING id"; // ถ้า billSaleId มีค่า = 0 ให้สร้างเวลาปัจจุบันและ return id ออกไป
                        cmd.ExecuteNonQuery();
                        int result = cmd.ExecuteNonQuery(); //คิวลี่ใส่ในผลลัพธ์

                        return Ok(new { id = result }); //นำผลลัพธ์ใส่ใน id แล้ว return ออกมา
                    }
                }
                // 3 create bill sale detail
                {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO tb_bill_sale_detail(bill_sale_id, book_id, qty, price)
                        VALUES(@bill_sale_id, @book_id, 1, @price)
                    ";
                    cmd.Parameters.AddWithValue("bill_sale_id", billSaleId);
                    cmd.Parameters.AddWithValue("book_id", id);
                    cmd.Parameters.AddWithValue("price", price);

                    if (cmd.ExecuteNonQuery() != -1) {
                        return Ok(new { message = "success", billSaleId = billSaleId });
                    }
                }
                return Ok();
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
        }
    }
}