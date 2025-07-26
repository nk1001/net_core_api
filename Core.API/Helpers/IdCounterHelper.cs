using Core.EF.Infrastructure.Services;
using Core.Entity.Model.Systems;
using Microsoft.EntityFrameworkCore;

namespace Core.API.Helpers
{
    public class IdCounterHelper
    {
        private static IServiceBase<SysCounter> _counterService;
        public IdCounterHelper(IServiceBase<SysCounter> counterService)
        {
            _counterService = counterService;
        }
        /// <summary>
        /// Author: SonTM 29/03/24 - Lấy cảm hứng từ dự án của anh LộcLM
        /// Hàm cấp số tự tăng theo format cung cấp
        /// LƯU Ý sử dụng hàm này BÊN NGOÀI TRANSACTION 
        /// Tạm thời sử dụng mutext để block thread vì trường hợp có > 1 request ở cùng 1 thời điểm
        /// thì counter sẽ xung đột khi ghi đè giá trị ở > 1 thread
        /// chỗ này nếu deploy load balancing ở tầng infra thì phải sửa lại blocking ở tầng database (block update row)
        /// </summary>
        /// <param name="key">Key để hệ thống gen cho 1 nghiệp vụ nào đó ví dụ SoPhieu_PhieuChi_BLVP</param>
        /// <param name="numberFormat">format số tự tăng theo chuỗi truyền vào</param>
        /// <param name="format">Cấu hình prefix hoặc postfix cho mã được tạo ví dụ PC.{0}</param>
        /// <returns></returns>
        private static SemaphoreSlim _mutext = new SemaphoreSlim(1);
        public static async Task<string> CapMa(string key, string numberFormat = "00000000", string format = "{0}")
        {
            await _mutext.WaitAsync();
         
            var currentCounter = await _counterService.Queryable().Where(x => x.ID.Equals(key)).FirstOrDefaultAsync();
            if (currentCounter == null)
            {
                currentCounter = new SysCounter()
                {
                    Count = 1,
                    NumberFormat = numberFormat,
                    Format = format,
                    ID = key,
                    Status = 1
                };
               await  _counterService.AddAsync(currentCounter);
            }
            else
            {
                currentCounter.Count += 1;
                await _counterService.UpdateAsync(currentCounter);
            }
            var ma = string.Format(currentCounter.Format, currentCounter.Count?.ToString(currentCounter.NumberFormat));
            _mutext.Release();
            return ma;
        }
    }
}
