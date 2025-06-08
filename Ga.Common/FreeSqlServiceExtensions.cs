using FreeSql;
using Microsoft.Extensions.DependencyInjection;

namespace Ga.Common;


/// <summary>
/// BootstrapBlazor 服务扩展类
/// </summary>
public static class FreeSqlServiceExtensions
{
    /// <summary>
    /// 增加 FreeSql 数据库操作服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsAction"></param>
    /// <param name="configureAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddFreeSql(this IServiceCollection services, Action<FreeSqlBuilder> optionsAction, Action<IFreeSql>? configureAction = null)
    {
        var builder = new FreeSqlBuilder();

        builder.UseAutoSyncStructure(true);
        builder.UseNameConvert(FreeSql.Internal.NameConvertType.PascalCaseToUnderscoreWithLower);
        builder.UseExitAutoDisposePool(false);

        optionsAction(builder);
        var instance = builder.Build();
        configureAction?.Invoke(instance);

        instance.UseJsonMap(); //开启功能
        BaseEntity.Initialization(instance, null);

        services.AddSingleton<IFreeSql>(instance);
        return services;
    }
}