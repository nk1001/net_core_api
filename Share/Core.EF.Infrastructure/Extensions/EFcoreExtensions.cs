using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Core.Helper.Extend;

namespace Core.EF.Infrastructure.Extensions
{
    public static class EFcoreExtensions
    {
        public static Expression<Func<T, bool>>? BuildFindKey<T>(this DbContext context,object[] objects)
        {
            if (context.Model.FindEntityType(typeof(T))!.FindPrimaryKey() == null)
            {
                return null;
            }
            var keyName = context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties
                .Select(x => x.Name);

            var pe = Expression.Parameter(typeof(T), "item");
            var list = new List<Expression>();
            var count = 0;

            foreach (var key in keyName)
            {
                var pro = Expression.Property(pe, key);
                var converter = TypeDescriptor.GetConverter(pro.Type);
                var value = converter.ConvertFrom(objects[count]);
                var exp = Expression.Equal(pro, Expression.Constant(value));
                list.Add(exp);
                count++;
            }
            Expression? expOut = null;
            foreach (var exp in list)
            {
                if (expOut == null)
                {
                    expOut = exp;
                }
                else
                {
                    expOut = Expression.AndAlso(expOut, exp);
                }
            }
            return Expression.Lambda<Func<T, bool>>(expOut!, pe);
        }
        public static Expression<Func<T, bool>>? BuildFindKey<T>(this DbContext context, T item)
        {
            if (context.Model.FindEntityType(typeof(T))!.FindPrimaryKey() == null)
            {
                return null;
            }
            var keyName = context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties
                .Select(x => x.Name);

            var pe = Expression.Parameter(typeof(T), "item");
            var list = new List<Expression>();
            foreach (var key in keyName)
            {
                var pro = Expression.Property(pe, key);
                var exp = Expression.Equal(pro, Expression.Constant(item.GetPropValue(key)));
                list.Add(exp);
            }
            Expression? expOut = null;
            foreach (var exp in list)
            {
                if (expOut == null)
                {
                    expOut = exp;
                }
                else
                {
                    expOut = Expression.AndAlso(expOut, exp);
                }
            }
            return Expression.Lambda<Func<T, bool>>(expOut!, pe);
        }

        public static async Task<List<string>> GetDatabaseSchema(this DbContext context, string schema)
        {
            // Chuẩn bị câu truy vấn SQL
            string sql = "SELECT USERNAME FROM DBA_USERS WHERE USERNAME LIKE :Schema";

            // Danh sách kết quả
            List<string> lst = new List<string>();

            try
            {
                // Tạo và cấu hình command
                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;

                    // Thêm tham số để tránh SQL injection
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = ":Schema"; // Oracle sử dụng dấu ":" thay vì "@"
                    parameter.Value = schema.ToUpper() + "%";
                    command.Parameters.Add(parameter);

                    // Mở kết nối
                    await context.Database.OpenConnectionAsync();

                    // Thực thi và đọc kết quả
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lst.Add(reader.GetString(0)); // USERNAME nằm ở cột đầu tiên
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi hoặc xử lý lỗi theo yêu cầu của bạn
                Console.Error.WriteLine($"Error fetching database schema: {ex.Message}");
                throw; // Tuỳ chọn: ném lại ngoại lệ nếu cần
            }
            finally
            {
                // Đảm bảo đóng kết nối
                await context.Database.CloseConnectionAsync();
            }

            return lst;
        }

    }
}
