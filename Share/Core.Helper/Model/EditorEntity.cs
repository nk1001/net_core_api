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
        [NotMapped]
        public ActionType? ActionType { get; set; }
    }
    public enum ActionType
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        Design = 4,
        Publish = 5,
        Unattached = 6,
        Continue = 7
    }
    public interface ICompanyBaseEntity
    {
         int CompanyID { get; set; }
    }
  
    public interface ICaching
    {
        bool IsCaching { get; set; }

    }

}
