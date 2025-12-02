// Models/EditPropertyDto.cs
using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class EditPropertyDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Инвентарный номер обязателен")]
        [StringLength(100, ErrorMessage = "Инвентарный номер не может превышать 100 символов")]
        [Display(Name = "Инвентарный номер")]
        public string InventoryNumber { get; set; }

        [Display(Name = "Дата баланса")]
        public DateTime? BalanceDate { get; set; }

        [Display(Name = "Срок использования (месяцев)")]
        [Range(0, 1000, ErrorMessage = "Срок использования должен быть от 0 до 1000 месяцев")]
        public int? UsagePeriod { get; set; }

        [Display(Name = "Стоимость")]
        [Range(0, double.MaxValue, ErrorMessage = "Стоимость не может быть отрицательной")]
        public decimal? Cost { get; set; }

        [Display(Name = "Дата последнего обслуживания")]
        public DateTime? LastMaintenanceDate { get; set; }

        [Display(Name = "Срок годности")]
        public DateTime? ExpiryDate { get; set; }

        [Required(ErrorMessage = "Тип имущества обязателен")]
        [Display(Name = "Тип имущества")]
        public int PropertyTypeId { get; set; }

        [Required(ErrorMessage = "Размещение обязательно")]
        [Display(Name = "Размещение")]
        public int LocationId { get; set; }

        [Display(Name = "Назначенный пользователь")]
        public int? AssignedUserId { get; set; }
    }
}