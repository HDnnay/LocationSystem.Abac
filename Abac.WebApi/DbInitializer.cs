using Abac.WebApi.Models;

namespace Abac.WebApi
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // 添加测试用户
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User { UserName = "admin", Password = "admin", Department = "IT", Level = 10, Roles = new List<string> { "Admin" } },
                    new User { UserName = "zhang", Password = "zhang", Department = "销售部", Level = 5, Roles = new List<string> { "Manager" } },
                    new User { UserName = "li", Password = "li", Department = "销售部", Level = 3, Roles = new List<string> { "Employee" } },
                    new User { UserName = "wang", Password = "wang", Department = "财务部", Level = 4, Roles = new List<string> { "Employee" } }
                );
            }
            context.SaveChanges();
            Task.Delay(10);
            // 添加测试文档
            if (!context.Documents.Any())
            {
                var userId = context.Users.FirstOrDefault(t => t.Department=="销售部")?.Id;
                var userId2 = context.Users.FirstOrDefault(t => t.Department=="财务部")?.Id;
                var userId3 = context.Users.FirstOrDefault(t => t.Department=="销售部")?.Id;

                context.Documents.AddRange(
                    new Document { Title = "销售报告1", Content = "内容1", OwnerId = userId.Value, Department = "销售部", Status = "Draft", Confidentiality = "Low" },
                    new Document { Title = "财务报告", Content = "内容2", OwnerId = userId2.Value, Department = "财务部", Status = "Approved", Confidentiality = "High" },
                    new Document { Title = "销售报告2", Content = "内容3", OwnerId = userId3.Value, Department = "销售部", Status = "Draft", Confidentiality = "Medium" }
                );
            }

            // 添加ABAC规则
            if (!context.Policies.Any())
            {
                context.Policies.AddRange(
                    new Policy { ResourceType = "Document", RuleExpression = "User.Roles.Contains(\"Admin\")", Effect = "Allow", Priority = 0 },
                    new Policy { ResourceType = "Document", RuleExpression = "Resource.Confidentiality == \"High\" && (Environment.CurrentTime.Hour < 9 || Environment.CurrentTime.Hour > 18)", Effect = "Deny", Priority = 5 },
                    new Policy { ResourceType = "Document", RuleExpression = "User.Roles.Contains(\"Manager\") && Resource.Department == User.Department", Effect = "Allow", Priority = 10 },
                    new Policy { ResourceType = "Document", RuleExpression = "Resource.OwnerId == User.Id", Effect = "Allow", Priority = 20 }
                );
            }
            context.SaveChanges();

        }
    }
}
