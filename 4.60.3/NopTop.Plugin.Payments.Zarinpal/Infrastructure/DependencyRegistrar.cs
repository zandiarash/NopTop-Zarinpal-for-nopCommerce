// using Autofac;
// using Autofac.Core;
// using Nop.Core.Caching;
// using Nop.Core.Configuration;
// using Nop.Core.Data;
// using Nop.Core.Infrastructure;
// using Nop.Core.Infrastructure.DependencyManagement;
// using Nop.Data;
// using Nop.Services.Orders;
// using Nop.Web.Framework.Infrastructure.Extensions;

// namespace NopTop.Plugin.Payments.Zarinpal.Infrastructure
// {
//     /// <summary>
//     /// Dependency registrar of the Avalara tax provider services
//     /// </summary>
//     public class DependencyRegistrar : IDependencyRegistrar
//     {
//         /// <summary>
//         /// Register services and interfaces
//         /// </summary>
//         /// <param name="builder">Container builder</param>
//         /// <param name="typeFinder">Type finder</param>
//         /// <param name="config">Config</param>
//         public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
//         {
//         //   builder.RegisterType<PerRequestCacheManager>().As<ICacheManager>().Named<ICacheManager>("nop_cache_per_request").InstancePerRequest();
//         //   builder.RegisterType<MemoryCacheManager>().As<ICacheManager>().Named<ICacheManager>("nop_cache_static").SingleInstance();
//         //   builder.RegisterType<ZarinpalHelper>().As<ZarinpalHelper>()
//         //     .WithParameter(ResolvedParameter.ForNamed<ICacheManager>("nop_cache_static"))
//         //     .InstancePerRequest();
//         }

//         public int Order => 4;
//     }
// }