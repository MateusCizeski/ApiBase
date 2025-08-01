﻿namespace ApiBase.Core.src.Infra.Query
{
    public class QueryBuilder<T>
    {
        public IQueryable<T> Query { get; set; }
        public void Build(IQueryable<T> query, List<FilterGroup> filters, List<SortModel> sorters)
        {
            query = new WhereQuery().BuildWhere(query, filters);
            query = new OrderByQuery().BuildOrderBy(query, sorters);
            Query = query;
        }
    }
}
