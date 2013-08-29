using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Diagnostics;

namespace EntityFrameworkExample1
{

    interface IContextProvider
    {
        MyContext GetContext();
    }

    public class Account
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
    }

    public class Contact
    {
        public int ContactId { get; set; }
        public string Name { get; set; }
        public Account Account { get; set; }
    }

    public class MyContext : DbContext
    {
        public MyContext(string ConnectionString) : base(ConnectionString) { }

        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Account> Accounts { get; set; }
    }

    class DynamicContextProvider : IContextProvider
    {
        private readonly string m_connectionString;

        public DynamicContextProvider(string connectionString)
        {
            m_connectionString = connectionString;
        }

        public MyContext GetContext()
        {
            return new MyContext(m_connectionString);
        }
    }

    class StaticContextProvider : IContextProvider
    {
        private MyContext m_context;

        public StaticContextProvider(string connectionString)
        {
            m_context = new MyContext(connectionString);
        }

        public MyContext GetContext()
        {
            return m_context;
        }
    }

    class Program
    {
        static void Main()
        {
            var staticProvider = new StaticContextProvider(Settings.Default.ConnectionString);

            var dynamicProvider = new DynamicContextProvider(Settings.Default.ConnectionString);

            SetupDatabase(dynamicProvider);

            TimeTest("Test A - use caching", () => RunTest(staticProvider));

            TimeTest("Test B - no caching", () => RunTest(dynamicProvider));

            Console.WriteLine("Done");
            Console.Read();
        }

        private static void SetupDatabase(DynamicContextProvider dynamicProvider)
        {
            using (var ctx = dynamicProvider.GetContext())
            {
                RecreateDatabase(ctx);
                AddSomeContacts(ctx);
            }
        }

        private static void RunTest(IContextProvider contextProvider)
        {
            List<Contact> contacts = new List<Contact>();

            var ctx = contextProvider.GetContext();
            contacts = ctx.Contacts.ToList();

            foreach (var contact in contacts.Take(2000))
            {
                ctx = contextProvider.GetContext();

                // get a copy of the contact - either new or cached depending on our context provider
                var localContact = ctx.Contacts.Single(c => c.ContactId == contact.ContactId);
                // create a new account
                Account acc = new Account() { Name = contact.Name };
                // link contact and account
                localContact.Account = acc;
                // save changes
                ctx.SaveChanges();
            }

        }

        private static void TimeTest(string testName, Action action)
        {
            Console.WriteLine();
            Console.WriteLine(testName);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine("{0:N0}ms", stopwatch.ElapsedMilliseconds);            
        }

        private static void AddSomeContacts(MyContext ctx)
        {
            Console.WriteLine("Adding Contacts");
            for (int i = 0; i < 4000; i++)
            {
                if ((i+1)%1000 == 0)
                {
                    Console.WriteLine(i+1);
                }
                ctx.Contacts.Add(new Contact() {Name = String.Format("Contact {0}", i)});
            }
            Console.WriteLine("Saving Changes");
            ctx.SaveChanges();
        }

        private static void RecreateDatabase(MyContext ctx)
        {
            Console.WriteLine("Database " + (ctx.Database.Exists() ? "Exists" : "Does Not Exist"));
            if (ctx.Database.Exists())
            {
                Console.WriteLine("Deleting Database");
                ctx.Database.Delete();
            }
            Console.WriteLine("Creating Database");
            ctx.Database.Create();
        }
    }
}
