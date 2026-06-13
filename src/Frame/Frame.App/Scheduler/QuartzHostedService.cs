// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Options;
// using Quartz;
// using Quartz.Spi;
//
// namespace Frame.App.Sheduler;
//
// public class QuartzHostedService : IHostedService
// {
//     private readonly ISchedulerFactory _schedulerFactory;
//     // Quartz setting to read configured value from appsetting
//     private readonly IOptions<QuartzSettings> _quartzSettings;
//     private readonly IJobFactory _jobFactory;
//     private IScheduler _scheduler;
//
//     public QuartzHostedService(
//         ISchedulerFactory schedulerFactory,
//         IJobFactory jobFactory,
//         IOptions<QuartzSettings> quartzSettings)
//     {
//         _schedulerFactory = schedulerFactory;
//         _jobFactory = jobFactory;
//         _quartzSettings = quartzSettings;
//     }
//
//
//     public async Task StartAsync(CancellationToken cancellationToken)
//     {
//         _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
//         _scheduler.JobFactory = _jobFactory;
//
//         var jobDetail = JobBuilder.Create<AlarmJob>()
//             .WithIdentity(_quartzSettings.Value.JobName, _quartzSettings.Value.JobGroup)         
//             .Build();
//
//
//         var trigger = TriggerBuilder.Create()
//                 .WithIdentity($"{_quartzSettings.Value.JobName}Trigger", _quartzSettings.Value.JobGroup)
//                 .WithCronSchedule(_quartzSettings.Value.CronSchedule) .WithMisfireHandlingInstructionFireNow()) 
//             .Build();
//
//         await _scheduler.ScheduleJob(jobDetail, trigger);
//         await _scheduler.Start();
//     }
//
//     public async Task StopAsync(CancellationToken cancellationToken)
//     {
//         if (_scheduler != null)
//         {
//             await _scheduler.Shutdown(cancellationToken);
//         }
//     }
// }