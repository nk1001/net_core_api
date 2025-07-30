using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.IOC;

namespace Core.Helper.Model
{
    [Serializable]
    public abstract class EditorEntity: IDependencyService
    {
        public int? Status { get; set; }
    
        public string? CreateBy { get; set; }
       
        public string? CreatebyName { get; set; }
     
        public DateTime? CreateDate { get; set; }
      
        public string? LastUpdateBy { get; set; }
     
        public string? LastUpdateByName { get; set; }
       
        public DateTime? LastUpdateDate { get; set; }      
    }
    
    public interface ICompanyBaseEntity
    {
         int? CompanyID { get; set; }
    }
  
    public interface ICaching
    {
        bool IsCaching { get; set; }

    }

}
