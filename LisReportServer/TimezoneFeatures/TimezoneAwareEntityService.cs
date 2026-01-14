using LisReportServer.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace LisReportServer.Services
{
    /// <summary>
    /// 时区感知的实体服务，用于处理包含时间字段的实体的时区转换
    /// </summary>
    public interface ITimezoneAwareEntityService
    {
        /// <summary>
        /// 将UTC时间字段转换为客户端时区时间
        /// </summary>
        T ConvertUtcFieldsToClientTime<T>(T entity) where T : class;
        
        /// <summary>
        /// 将实体集合中的UTC时间字段转换为客户端时区时间
        /// </summary>
        IEnumerable<T> ConvertUtcFieldsToClientTime<T>(IEnumerable<T> entities) where T : class;
        
        /// <summary>
        /// 从客户端时区时间转换为UTC时间（用于保存到数据库）
        /// </summary>
        T ConvertClientTimeToUtc<T>(T entity) where T : class;
    }

    public class TimezoneAwareEntityService : ITimezoneAwareEntityService
    {
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<TimezoneAwareEntityService> _logger;
        
        // 缓存实体类型的时间属性信息，避免重复反射
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _cachedTimeProperties = 
            new ConcurrentDictionary<Type, PropertyInfo[]>();

        public TimezoneAwareEntityService(ITimezoneService timezoneService, ILogger<TimezoneAwareEntityService> logger)
        {
            _timezoneService = timezoneService;
            _logger = logger;
        }

        public T ConvertUtcFieldsToClientTime<T>(T entity) where T : class
        {
            if (entity == null) return null;

            try
            {
                var entityType = typeof(T);
                var properties = GetCachedTimeProperties(entityType);

                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    if (value != null)
                    {
                        if (value is DateTime dateTime)
                        {
                            var clientTime = _timezoneService.ConvertUtcToClientTime(dateTime);
                            property.SetValue(entity, clientTime);
                        }
                        else if (value is DateTime? && ((DateTime?)value).HasValue)
                        {
                            var clientTime = _timezoneService.ConvertUtcToClientTime(((DateTime?)value).Value);
                            property.SetValue(entity, clientTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting UTC fields to client time for type {TypeName}", typeof(T).Name);
            }

            return entity;
        }
        
        private PropertyInfo[] GetCachedTimeProperties(Type entityType)
        {
            return _cachedTimeProperties.GetOrAdd(entityType, type =>
            {
                return type.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                    .ToArray();
            });
        }

        public IEnumerable<T> ConvertUtcFieldsToClientTime<T>(IEnumerable<T> entities) where T : class
        {
            if (entities == null) return null;

            // 使用LINQ进行转换，避免创建不必要的中间列表
            return entities.Select(ConvertUtcFieldsToClientTime);
        }

        public T ConvertClientTimeToUtc<T>(T entity) where T : class
        {
            if (entity == null) return null;

            try
            {
                var entityType = typeof(T);
                var properties = GetCachedTimeProperties(entityType);

                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    if (value != null)
                    {
                        if (value is DateTime dateTime)
                        {
                            var utcTime = ConvertToUtcTime(dateTime);
                            property.SetValue(entity, utcTime);
                        }
                        else if (value is DateTime? && ((DateTime?)value).HasValue)
                        {
                            var nullableValue = (DateTime?)value;
                            var utcTime = ConvertToUtcTime(nullableValue.Value);
                            property.SetValue(entity, new DateTime?(utcTime));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting client time to UTC for type {TypeName}", typeof(T).Name);
            }

            return entity;
        }
        
        private DateTime ConvertToUtcTime(DateTime dateTime)
        {
            // 假设传入的是本地时间，需要转换为UTC
            var utcTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // 如果没有指定Kind，则假设为本地时间
                var localTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, TimeZoneInfo.Local);
            }
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.Local);
            }
            
            return utcTime;
        }
    }
}