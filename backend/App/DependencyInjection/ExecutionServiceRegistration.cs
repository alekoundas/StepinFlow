using Business.Services.ExecutionService;
using Business.Services.ExecutionService.Workers;
using Core.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace App.DependencyInjection
{
    /// <summary>
    /// Where the step types are wired to their workers.
    ///
    /// The map is built here rather than inside the factory so the factory needs no container, and
    /// so the whole type to worker relationship is on one screen. It lives in App because knowing
    /// about the container is the composition root's job and nobody else's.
    ///
    /// Workers are singletons because they hold no state. If one ever needs per-step state, that
    /// stops being true and this is where it breaks.
    /// </summary>
    public static class ExecutionServiceRegistration
    {
        public static IServiceCollection AddExecutionEngine(this IServiceCollection services)
        {
            services.AddSingleton<PassThroughStepWorker>();
            services.AddSingleton<WaitStepWorker>();
            services.AddSingleton<NotifyStepWorker>();
            services.AddSingleton<CursorStepWorker>();
            services.AddSingleton<WindowStepWorker>();
            services.AddSingleton<KeyboardStepWorker>();
            services.AddSingleton<ImageSearchStepWorker>();
            services.AddSingleton<ReadTextStepWorker>();
            services.AddSingleton<CheckValueStepWorker>();
            services.AddSingleton<SystemCommandStepWorker>();
            services.AddSingleton<SystemActionStepWorker>();

            services.AddSingleton<IStepWorkerFactory>(x => new StepWorkerFactory(
                new Dictionary<FlowStepTypeEnum, IStepWorker>
                {
                    [FlowStepTypeEnum.WAIT] = x.GetRequiredService<WaitStepWorker>(),
                    [FlowStepTypeEnum.NOTIFY] = x.GetRequiredService<NotifyStepWorker>(),

                    [FlowStepTypeEnum.CURSOR_RELOCATE] = x.GetRequiredService<CursorStepWorker>(),
                    [FlowStepTypeEnum.CURSOR_CLICK] = x.GetRequiredService<CursorStepWorker>(),
                    [FlowStepTypeEnum.CURSOR_SCROLL] = x.GetRequiredService<CursorStepWorker>(),
                    [FlowStepTypeEnum.CURSOR_DRAG] = x.GetRequiredService<CursorStepWorker>(),

                    [FlowStepTypeEnum.WINDOW_FOCUS] = x.GetRequiredService<WindowStepWorker>(),
                    [FlowStepTypeEnum.WINDOW_RESIZE] = x.GetRequiredService<WindowStepWorker>(),
                    [FlowStepTypeEnum.WINDOW_RELOCATE] = x.GetRequiredService<WindowStepWorker>(),

                    [FlowStepTypeEnum.KEYBOARD_INPUT] = x.GetRequiredService<KeyboardStepWorker>(),
                    [FlowStepTypeEnum.IMAGE_SEARCH] = x.GetRequiredService<ImageSearchStepWorker>(),
                    [FlowStepTypeEnum.READ_TEXT] = x.GetRequiredService<ReadTextStepWorker>(),
                    [FlowStepTypeEnum.CHECK_VALUE] = x.GetRequiredService<CheckValueStepWorker>(),
                    [FlowStepTypeEnum.SYSTEM_COMMAND] = x.GetRequiredService<SystemCommandStepWorker>(),
                    [FlowStepTypeEnum.SYSTEM_ACTION] = x.GetRequiredService<SystemActionStepWorker>(),
                },
                x.GetRequiredService<PassThroughStepWorker>()));

            services.AddSingleton<IExecutionCacheService, ExecutionCacheService>();
            services.AddSingleton<IExecutionEngine, ExecutionEngine>();

            return services;
        }
    }
}
