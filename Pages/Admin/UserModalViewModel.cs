using WebPepperCan.Data;

namespace WebPepperCan.Pages.Admin
{
    public class UserModalViewModel
    {
        public string ModalId { get; set; }
        public string FormId { get; set; }
        public string Title { get; set; }
        public string SubmitButtonText { get; set; }
        public bool IsEdit { get; set; }
        public ApplicationUser User { get; set; }
        public IEnumerable<Organization> Organizations { get; set; }
    }
}
