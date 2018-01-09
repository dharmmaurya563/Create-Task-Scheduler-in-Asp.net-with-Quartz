# Create-Task-Scheduler-in-Asp.net-with-Quartz
//Call EmailJob
            // AddJob(dt_Data);

Create Task Scheduler in Asp.net
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
