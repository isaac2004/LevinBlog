﻿using LevinBlog.Database;
using LevinBlog.Database.Entity;
using LevinBlog.Model;
using System.Collections.Generic;
using System.Linq;

namespace LevinBlog.Repository
{
    public interface ICategoryRepository
    {
        IEnumerable<CategoryEntity> GetAll();

        IEnumerable<CategoryEntity> GetAllPaged(int count, int page);

        CategoryEntity GetById(int id);

        CategoryEntity Add(CategoryEntity categoryEntity);

        void Update(Category category);

        void Remove(int id);
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly BlogContext _blogContext;

        public CategoryRepository(BlogContext blogContext)
        {
            _blogContext = blogContext;
        }

        /// <summary>
        /// Get all Categories
        /// </summary>
        /// <returns>Category Collection</returns>
        public IEnumerable<CategoryEntity> GetAll()
        {
            return _blogContext
                      .Categories
                      .ToList();
        }

        /// <summary>
        /// Get Category By Id
        /// </summary>
        /// <param name="id">Category Id</param>
        /// <returns>Category Entity</returns>
        public CategoryEntity GetById(int id)
        {
            return _blogContext
                    .Categories
                    .FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// Create Category
        /// </summary>
        /// <param name="categoryEntity">Category Entity to save</param>
        /// <returns>Category Entity with Id</returns>
        public CategoryEntity Add(CategoryEntity categoryEntity)
        {
            _blogContext
                .Categories
                .Add(categoryEntity);
            _blogContext.SaveChanges();

            return categoryEntity;
        }

        /// <summary>
        /// Update Entity based on Model
        /// </summary>
        /// <param name="category">Category Object</param>
        public void Update(Category category)
        {
            var entity = _blogContext
                .Categories
                .FirstOrDefault(x => x.Id == category.Id);

            entity.Url = category.Url;
            entity.Name = category.Name;
            entity.IsActive = category.IsActive;
            _blogContext.SaveChanges();
        }

        /// <summary>
        /// Remove Category record based on Id
        /// </summary>
        /// <param name="id">Category Id</param>
        public void Remove(int id)
        {
            var entity = _blogContext
                .Categories
                .FirstOrDefault(x => x.Id == id);
            entity.IsActive = false;

            _blogContext.SaveChanges();
        }

        /// <summary>
        /// Get a collection of categories by skipping x and taking y
        /// </summary>
        /// <param name="count">The total number of categories you want to Take</param>
        /// <param name="page">The denomination of categories you want to skip. (page - 1) * count </param>
        /// <returns>Collections of Categories</returns>
        public IEnumerable<CategoryEntity> GetAllPaged(int count, int page)
        {
            return _blogContext
                    .Categories
                    .Skip((page - 1) * count)
                    .Take(count)
                    .ToList();
        }
    }
}