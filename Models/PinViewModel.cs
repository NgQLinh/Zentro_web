using System.ComponentModel.DataAnnotations;

namespace Zentro.Models
{
    public class PinViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã PIN")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã PIN phải gồm 6 chữ số")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã PIN phải gồm 6 chữ số")]
        [DataType(DataType.Password)]
        public string Pin { get; set; } = string.Empty;
    }
}