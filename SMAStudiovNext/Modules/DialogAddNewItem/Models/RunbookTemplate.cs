namespace SMAStudiovNext.Modules.DialogAddNewItem.Models
{
    public class RunbookTemplate
    {
        public RunbookTemplate()
        {
            Type = "Runbook Template";
        }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }
    }
}
