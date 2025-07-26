using System.ComponentModel;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Core.Helper.APIMessage;
using Core.Helper.Extend;
using Core.Helper.IService;
using Core.EF.Infrastructure.Extensions;
using Core.EF.WebApi.Helper;

namespace Core.EF.WebApi.Controllers
{

    [Authorize]
    [Route("api/[controller]",Order = 99)]
    [ApiController]
    public abstract class BaseApiController<TEntity> : ControllerBase where TEntity : class
    {
        public readonly Infrastructure.Services.IServiceBase<TEntity> IService;
        public readonly ILogger<TEntity> ILogger;

        protected BaseApiController(Infrastructure.Services.IServiceBase<TEntity> service,  ILogger<TEntity> logger)
        {
            IService = service;
            ILogger = logger;
        }
        /// <summary>
        /// Tìm kiếm dữ liệu & phân trang
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("Filter")]
        [ProducesDefaultResponseType(typeof(MessageResponse<>))]
        public virtual async Task<IActionResult> Filter(FilterMessage filter)
        {
            try
            {
                var pe = Expression.Parameter(typeof(TEntity), "item");
                if (filter.Parameters != null)
                {
                    var all = ExpressionHelper.BuildExpression<TEntity>(filter.Parameters, pe);
                    if (all != null)
                    {
                        var cExp = Expression.Lambda<Func<TEntity, bool>>(all, pe);

                        if (filter.PageIndex==int.MaxValue)
                        {
                            var rs = await IService.GetAsync(cExp);
                            if (filter.OrderBy!=null && filter.OrderByMethod!=null)
                            {
                                rs = rs.OrderBy(new List<OrderByInfo>()
                                {
                                    new OrderByInfo
                                    {
                                        Initial = true,
                                        PropertyName = filter.OrderBy,
                                        Direction = filter.OrderByMethod == "Descending"
                                            ? SortDirection.Descending
                                            : SortDirection.Ascending,

                                    }
                                }).ToList();
                            }
                            return Ok(
                                new MessageResponse<FilterMessageResponse<TEntity>>
                                {
                                    Data = new FilterMessageResponse<TEntity>(filter, rs.Count, rs)
                                });
                        }
          
                        if (filter.OrderBy!=null)
                        {
                            var rs = await IService.GetAsync(cExp, new List<OrderByInfo>(){new()
                            {
                                Initial = true,
                                PropertyName = filter.OrderBy,
                                Direction = filter.OrderByMethod=="Descending"?SortDirection.Descending:SortDirection.Ascending,

                            }}, filter.PageSize, filter.PageIndex);
                         
                            return Ok(
                                new MessageResponse<FilterMessageResponse<TEntity>>
                                {
                                    Data = new FilterMessageResponse<TEntity>(filter, rs.Item2, rs.Item1)
                                });
                        }

                    }
                    else
                    {
                        return Ok(
                            new MessageResponse<FilterMessageResponse<TEntity>>
                            {
                                Status = 400,
                                Data = new FilterMessageResponse<TEntity>(filter,-1,new List<TEntity>()),
                                Message = "Condition build failed"
                            });
                    }

                }
                return Ok(
                    new MessageResponse<FilterMessageResponse<TEntity>>
                    {
                        Status = 400,
                        Data = null,
                        Message = "Parameters is null Or OrderBy is null"
                    });
            }
            catch (Exception e)
            {
                return Ok(
                    new MessageResponse<FilterMessageResponse<TEntity>>
                    {
                        Status = 500,
                        Data = null,
                        Message = e.Message
                    });
            }
           
        }
        /// <summary>
        /// Tìm kiếm LINQ & phân trang
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("FilterLinq")]
        [ProducesDefaultResponseType(typeof(MessageResponse<>))]
        public virtual async Task<IActionResult> FilterLinq(LinqFilterMessage filter)
        {
            try
            {
                
                if (filter.Parameters == null)
                    return Ok(
                        new MessageResponse<FilterLinqMessageResponse<TEntity>>
                        {
                            Status = 400,
                            Data = null,
                            Message = "Parameters is null"
                        });
                var constantExpressions = new Dictionary<string, object> {};
                foreach (var item in filter.Parameters)
                {
                    if (item.Type != null)
                    {
                        var type = Type.GetType(item.Type);
                        if (type!.IsArray || type.IsCollectionType() || type.IsEnumerableType())
                        {
                            if (item.Value != null)
                            {
                                var obj = JsonConvert.DeserializeObject(item.Value, type);
                                if (item.Name != null && obj!=null) constantExpressions.Add(item.Name, obj);
                            }
                        } 
                        else
                        {
                            if (item.Value != null)
                            {
                                if (type==typeof(DateTime) || type == typeof(DateTime?))
                                {
                                    var value = TypeDescriptor.GetConverter(type).ConvertFromString(item.Value.Replace("\"", ""));
                                    if (item.Name != null && value != null) constantExpressions.Add(item.Name, value);
                                }
                                else
                                {
                                    var value = TypeDescriptor.GetConverter(type).ConvertFromString(item.Value);
                                    if (item.Name != null && value != null) constantExpressions.Add(item.Name, value);
                                }
                              
                            }
                        }
                    }
                }

              
                Expression<Func<TEntity, bool>>? all = null;

                try
                {
                    all= DynamicExpressionParser.ParseLambda<TEntity, bool>(new ParsingConfig
                    {
                    }, true,  filter.Query, constantExpressions);
                }
                catch (Exception e)
                {
                    return Ok(
                        new MessageResponse<FilterLinqMessageResponse<TEntity>>
                        {
                            Status = 400,
                            Data = new(filter, -1, new List<TEntity>()),
                            Message = "Query is Error  - "+e.Message
                        });
                }
                var includes = filter.Includes==null? LinqIncludeExtensions.GetForeignKeyPaths(typeof(TEntity)): filter.Includes.Split(",").ToList();
                filter.Includes = string.Join(",", includes);

                if (filter.PageSize == int.MaxValue)
                {
                    List<TEntity> rs;
                    if (filter.FromDate != null && filter.ToDate!=null)
                    {
                        rs = await IService.GetAsyncSchema(all, filter.FromDate, filter.ToDate, includes: filter.Includes);
                        if (filter.OrderBy != null && filter.OrderByMethod != null)
                        {
                            rs = rs.AsQueryable().OrderBy(new List<OrderByInfo>()
                            {
                                new()
                                {
                                    Initial = true,
                                    PropertyName = filter.OrderBy,
                                    Direction = filter.OrderByMethod == "Descending"
                                        ? SortDirection.Descending
                                        : SortDirection.Ascending,

                                }
                            }).ToList();
                        }
                    }
                    else
                    {
                        rs = await IService.GetAsyncLinqToDB(all, includes: filter.Includes);
                        if (filter.OrderBy != null && filter.OrderByMethod != null)
                        {
                            rs = rs.AsQueryable().OrderBy(new List<OrderByInfo>()
                            {
                                new()
                                {
                                    Initial = true,
                                    PropertyName = filter.OrderBy,
                                    Direction = filter.OrderByMethod == "Descending"
                                        ? SortDirection.Descending
                                        : SortDirection.Ascending,

                                }
                            }).ToList();
                        }
                    }
                    
                    return Ok(
                        new MessageResponse<FilterLinqMessageResponse<TEntity>>
                        {
                            Data = new FilterLinqMessageResponse<TEntity>(filter, rs.Count, rs)
                        });
                }
                if (filter.OrderBy != null)
                {
                    (List<TEntity>, long) rs;
                    if (filter.FromDate!=null && filter.ToDate!=null)
                    {

                        rs = await IService.GetAsyncSchema(all, new List<OrderByInfo>()
                        {
                            new()

                            {
                                Initial = true,
                                PropertyName = filter.OrderBy,
                                Direction = filter.OrderByMethod == "Descending"
                                    ? SortDirection.Descending
                                    : SortDirection.Ascending,

                            }
                        }, filter.FromDate, filter.ToDate, filter.PageSize, filter.PageIndex,filter.Includes);


                    }
                    else
                    {
                        rs = await IService.GetAsyncLinqToDB(all, new List<OrderByInfo>(){new()

                        {
                            Initial = true,
                            PropertyName = filter.OrderBy,
                            Direction = filter.OrderByMethod=="Descending"?SortDirection.Descending:SortDirection.Ascending,

                        }}, filter.PageSize, filter.PageIndex, includes: filter.Includes);

                    }
                    
                    return Ok(
                        new MessageResponse<FilterLinqMessageResponse<TEntity>>
                        {
                            Data = new FilterLinqMessageResponse<TEntity>(filter, rs.Item2, rs.Item1)
                        });
                }
                return Ok(
                    new MessageResponse<FilterLinqMessageResponse<TEntity>>
                    {
                        Status = 400,
                        Data = new(filter, -1, new List<TEntity>()),
                        Message = "OrderBy is null"
                    });

            }
            catch (Exception e)
            {
                return Ok(
                    new MessageResponse<FilterLinqMessageResponse<TEntity>>
                    {
                        Status = 500,
                        Data = new FilterLinqMessageResponse<TEntity>(filter, -1, new List<TEntity>()),
                        Message = e.Message
                    });
            }

        }
        /// <summary>
        /// Lấy dữ liệu theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
       
        
        [HttpGet()]
        [ProducesDefaultResponseType(typeof(MessageResponse<>))]
        public virtual async Task<IActionResult> Get(string id)
        {
            try
            {

                var exp = IService.Repository.DbContext.BuildFindKey<TEntity>(new object[] { id });
                if (exp == null)
                {
                    return Ok(new MessageResponse<TEntity>
                    {
                        Data = null,
                        Message = "Không tìm thấy dữ liệu ID=" + id,
                        Status = 400
                    });
                }
                var state = IService.Repository.DbContext.ChangeTracker.QueryTrackingBehavior;
                IService.Repository.DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var rs = await IService.Repository.DbContext.Set<TEntity>().ToLinqToDBTable().AsNoTracking().Where(exp).LoadWithDynamic().FirstOrDefaultAsyncLinqToDB();
                IService.Repository.DbContext.ChangeTracker.QueryTrackingBehavior = state;


               // var rs = await IService.FindAsync(id);
                if (rs == null)
                {
                    return Ok(new MessageResponse<TEntity>
                    {
                        Data = null,
                        Message = "Không tìm thấy dữ liệu ID=" + id,
                        Status = 400
                    });
                }
                return Ok(new MessageResponse<TEntity>
                {
                    Data = rs,
                    Status = 200,

                });
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
        
        
        /// <summary>
        /// Thêm mới dữ liệu
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesDefaultResponseType(typeof(MessageResponse<>))]
        public virtual async Task<IActionResult> Post([FromBody] TEntity entity)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    var rs = await IService.AddAsync(entity);
                    if (rs)
                    {
                        return Ok(new MessageResponse<TEntity>
                        {
                            Data = entity,
                            Status = 200
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("Error", "Thêm dữ liệu thất bại");
                    }


                }
                return Ok(new MessageResponse<TEntity>
                {
                    Data = entity,
                    Errors = ModelState.Where(t => t.Value.Errors.Count > 0).
                        Select(t => new
                        {
                            Key = t.Key,
                            ErrorMessage = string.Join(",", t.Value.Errors.Select(tx => tx.ErrorMessage).ToList())
                        }).ToDictionary(t => t.Key, t => t.ErrorMessage),
                    Status = 400
                });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return Ok(new MessageResponse<TEntity>
                {
                    Data = entity,
                    Errors = ModelState.Where(t => t.Value.Errors.Count > 0).
                        Select(t => new
                        {
                            Key = t.Key,
                            ErrorMessage = string.Join(",", t.Value.Errors.Select(tx => tx.ErrorMessage).ToList())
                        }).ToDictionary(t => t.Key, t => t.ErrorMessage),
                    Status = 500
                });
            }
           
        }
        /// <summary>
        /// Cập nhật dữ liệu 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [HttpPut]
        [ProducesDefaultResponseType(typeof(MessageResponse<>))]
        public virtual async Task<IActionResult> Put([FromBody] TEntity entity)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var isUpdate = await IService.UpdateAsync(entity);
                    if (isUpdate)
                    {
                        return Ok(new MessageResponse<TEntity>
                        {
                            Data = entity,
                            Status = 200
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("Error", "Cập nhật dữ liệu thất bại");
                    }
                }
                return Ok(new MessageResponse<TEntity>
                {
                    Data = entity,
                    Errors = ModelState.Where(t => t.Value.Errors.Count > 0).
                        Select(t => new
                        {
                            Key = t.Key,
                            ErrorMessage = string.Join(",", t.Value.Errors.Select(tx => tx.ErrorMessage).ToList())
                        }).ToDictionary(t => t.Key, t => t.ErrorMessage),
                    Status = 400
                });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error",ex.Message);
                return Ok(new MessageResponse<TEntity>
                {
                    Data = entity,
                    Errors = ModelState.Where(t => t.Value.Errors.Count > 0).
                        Select(t => new
                        {
                            Key = t.Key,
                            ErrorMessage = string.Join(",", t.Value.Errors.Select(tx => tx.ErrorMessage).ToList())
                        }).ToDictionary(t => t.Key, t => t.ErrorMessage),
                    Status = 500
                });
            }
        }
        /// <summary>
        /// Xóa dữ liệu theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete()]
        [ProducesDefaultResponseType(typeof(MessageResponse<>))]
        public virtual async Task<IActionResult> Delete(string id)
        {
            var obj=await IService.FindAsync(id);
            if (obj!=null)
            {
                var rs = false;
                try
                {
                    obj.SetPropValue("Status", 3);
                    rs = await IService.DeleteAsync(obj);
                }
                catch (Exception e)
                {

                    ModelState.AddModelError("Error", e.Message);
                }

                if (rs)
                {
                    
                    
                    return Ok(new MessageResponse<TEntity>
                    {
                        Data = obj,
                        Status = 200
                    });
                }
                else
                {
                    ModelState.AddModelError("Error", "Xóa dữ liệu thất bại");
                    return Ok(new MessageResponse<TEntity>
                    {
                        Data = obj,
                        Errors = ModelState.Where(t => t.Value.Errors.Count > 0).
                            Select(t => new
                            {
                                Key = t.Key,
                                ErrorMessage = string.Join(",", t.Value.Errors.Select(tx => tx.ErrorMessage).ToList())
                            }).ToDictionary(t => t.Key, t => t.ErrorMessage),
                        Status = 400
                    });
                }

            }
            return Ok(new MessageResponse<TEntity>
            {
                Errors = ModelState.Where(t => t.Value.Errors.Count > 0).
                    Select(t => new
                    {
                        Key = t.Key,
                        ErrorMessage = string.Join(",", t.Value.Errors.Select(tx => tx.ErrorMessage).ToList())
                    }).ToDictionary(t => t.Key, t => t.ErrorMessage),
                Status = 404
            });
        }
    }
}
