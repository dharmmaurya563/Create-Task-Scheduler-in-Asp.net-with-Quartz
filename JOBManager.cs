using MySql.Data.MySqlClient;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

/// <summary>
/// SIPL-DK
///09-01-2018
/// Cart Scheduler Emailer foe :Pending payment anf Cart Items
/// </summary>
namespace EmailScheduler
{
    public class JOBManager
    {
        private static IScheduler _scheduler;
        public static string CartHours = ConfigurationManager.AppSettings["CartHours"];
        private static readonly String ConnectionString = ConfigurationManager.ConnectionStrings["Entities"].ConnectionString;
        public static string currentData = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        public static String SMTP_USERNAME = ConfigurationManager.AppSettings["AmazoneSES_SMTP_USERNAME"];
        public static String SMTP_PASSWORD = ConfigurationManager.AppSettings["AmazoneSES_SMTP_PASSWORD"];
        public static String HOST = ConfigurationManager.AppSettings["AmazoneSES_HOST"];
        public static String From_Email = ConfigurationManager.AppSettings["From_Email"];
        static void Main(string[] args)
        {
            //sendEmail();
            // Collect Data
            DataTable dt_Data = CollectData();
            TaskList(dt_Data);

            //while (true)
            //{
            //    Console.WriteLine("Doing work on the Main Thread !!");
            //}
            //Collect Data
            // DataTable dt_Data = CollectData();
            //Call EmailJob
            // AddJob(dt_Data);
        }
        public static void AddJob(DataTable dt_Data)
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();
            _scheduler.Start();
            //Console.WriteLine("Starting Scheduler");
            IJobDetail emailJob = JobBuilder.Create<EmailScheduler>().Build();
            if (dt_Data.Rows.Count > 10)
            {
                for (int i = 0; i < dt_Data.Rows.Count; i++)
                {
                    ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("cartEmail" + i, "group1")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(1)
                    .RepeatForever()
                    )
                    .Build();
                    try
                    {
                        //emailJob.JobDataMap["Id"] = dt_Data.Rows[i]["Id"];
                        //emailJob.JobDataMap["Email"] = dt_Data.Rows[i]["Email"].ToString();
                        //emailJob.JobDataMap["myStateData"] = "sss";

                        _scheduler.ScheduleJob(emailJob, trigger);
                    }
                    catch (Exception ex)
                    {
                        //log.Error("");
                        throw new ApplicationException("");
                    }
                }
            }

        }
        public static DataTable CollectData()
        {
            DataTable dt = new DataTable();
            try
            {
                using (TransactionScope transScope = new TransactionScope())
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmdData = new MySqlCommand("Proc_CartDataScheduler", conn);
                        cmdData.CommandType = CommandType.StoredProcedure;
                        cmdData.Parameters.AddWithValue("@currenctDate", currentData);
                        cmdData.Parameters.AddWithValue("@lastDate", currentData);
                        using (MySqlDataAdapter sda = new MySqlDataAdapter(cmdData))
                        {
                            sda.Fill(dt);
                        }
                    }
                    transScope.Complete();
                }
            }
            catch (Exception ex)
            {
                //Insert to Log Table
                var error = ex.ToString();
                //Insert to Log Table
                insertErrorLog(error, "Cart_Email_Scheduler:Fetch Data");
                //End Insert Log Here              
            }
            return dt;
        }

        public static async Task<int> TaskList(DataTable dt_Data)
        {
            await sendEmail(dt_Data);
            Console.WriteLine("Finished sending mail");
            return 1;
        }
        public static async Task sendEmail(DataTable dt_Data)
        {
            if (dt_Data.Rows.Count > 0)
            {
                await Task.Run(() =>//Task Run in Background
                {
                    for (int i = 0; i < dt_Data.Rows.Count; i++)
                    {
                        try
                        {
                            // Replace sender@example.com with your "From" address. 
                            // This address must be verified with Amazon SES.
                            const String FROM = "dharmveer.kushwaha@systematixindia.com";
                            const String FROMNAME = "Dharmveer";

                            // is still in the sandbox, this address must be verified.
                            String TO = Convert.ToString(dt_Data.Rows[i]["Email"]);

                            // (Optional) the name of a configuration set to use for this message.
                            // If you comment out this line, you also need to remove or comment out
                            // the "X-SES-CONFIGURATION-SET" header below.
                            // const String CONFIGSET = "ConfigSet";

                            // The port you will connect to on the Amazon SES SMTP endpoint. We
                            // are choosing port 587 because we will use STARTTLS to encrypt
                            // the connection.
                            const int PORT = 587;

                            // The subject line of the email
                            const String SUBJECT =
                                "Amazon SES test (SMTP interface accessed using C#)";

                            // The body of the email
                            const String BODY =
                                "<h1>Amazon SES Test</h1>" +
                                "<p>This email was sent through the " +
                                "<a href='https://aws.amazon.com/ses'>Amazon SES</a> SMTP interface " +
                                "using the .NET System.Net.Mail library.</p>";

                            // Create and build a new MailMessage object
                            MailMessage message = new MailMessage();
                            message.IsBodyHtml = true;
                            message.From = new MailAddress(FROM, FROMNAME);
                            message.To.Add(new MailAddress(TO));
                            message.Subject = SUBJECT;
                            message.Body = BODY;
                            // Comment or delete the next line if you are not using a configuration set
                            //message.Headers.Add("X-SES-CONFIGURATION-SET", CONFIGSET);    
                            // Create and configure a new SmtpClient
                            SmtpClient client = new SmtpClient(HOST, PORT);
                            // Pass SMTP credentials                 
                            client.Credentials = new NetworkCredential(SMTP_USERNAME, SMTP_PASSWORD); ;
                            // Enable SSL encryption
                            client.EnableSsl = true;
                            try
                            {
                                Console.WriteLine("Attempting to send email to " + TO + "....");
                                // client.Send(message);
                                client.Send(message);
                                Console.WriteLine("Email sent!");
                                //Update Cart Scheduler Table For sednEMail True
                                updateEmailTable(Convert.ToInt32(dt_Data.Rows[i]["Id"]), TO);
                                //End Here
                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                var error = ex.ToString();
                                //Insert to Log Table
                                insertErrorLog(error, "Cart_Email_Scheduler:Send Email:Email:" + TO + ":ID:" + dt_Data.Rows[i]["Id"] + "");
                                //End Insert Log Here 
                                Thread.Sleep(1000);
                                continue;
                            }
                            // await Task.Delay(5000);
                        }
                        catch (Exception ex)
                        {
                            var error = ex.ToString();
                            //Insert to Log Table
                            insertErrorLog(error, "Cart_Email_Scheduler:Send Email Loop");
                            //End Insert Log Here                                              
                        }
                    }
                });
            }
        }
        public static void updateEmailTable(int schId, string email)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                MySqlCommand cmdUpdate = new MySqlCommand("Proc_UpdateBFMe_CartScheduler", conn);
                cmdUpdate.CommandType = CommandType.StoredProcedure;
                cmdUpdate.Parameters.AddWithValue("@SchId", schId);
                cmdUpdate.Parameters.AddWithValue("@EmailId", email);
                cmdUpdate.ExecuteNonQuery();
            }
        }
        public static void insertErrorLog(string error,string methodName)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                MySqlCommand cmdInsert = new MySqlCommand("Proc_ErrorLog", conn);
                cmdInsert.CommandType = CommandType.StoredProcedure;
                cmdInsert.Parameters.AddWithValue("@methodName", methodName);
                cmdInsert.Parameters.AddWithValue("@error", error);
                cmdInsert.Parameters.AddWithValue("@currenctDate", currentData);
                cmdInsert.ExecuteNonQuery();
            }
        }

    }

}
