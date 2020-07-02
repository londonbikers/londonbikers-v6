using LBV6ForumApp;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace LBV6.Api
{
    public class CategoriesApiController : ApiController
    {
        // GET: api/categories
        public IEnumerable<CategoryDto> Get(bool extended = false)
        {
            // convert our domain categories to DTOs
            var dtos = Helpers.ConvertCategoriesToCategoryDtos(ForumServer.Instance.Categories.Categories, extended);
            return dtos;
        }

        // GET: api/categories/5
        [ResponseType(typeof(CategoryDto))]
        public IHttpActionResult Get(long id)
        {
            // convert our domain category to a DTO
            var category = ForumServer.Instance.Categories.Categories.SingleOrDefault(q => q.Id.Equals(id));
            if (category == null)
                return NotFound();

            var categoryDto = Helpers.ConvertCategoryToCategoryDto(category);
            return Ok(categoryDto);
        }

        // POST: api/categories
        [CheckModelForNull]
        [ResponseType(typeof(CategoryDto))]
        public async Task<IHttpActionResult> Post([FromBody]Category category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await ForumServer.Instance.Categories.UpdateCategoryAsync(category);
            var dto = Helpers.ConvertCategoryToCategoryDto(category);
            return Ok(dto);
        }

        // PUT: api/categories
        [CheckModelForNull]
        public async Task Put([FromBody]Category category)
        {
            await ForumServer.Instance.Categories.UpdateCategoryAsync(category);
        }

        // DELETE: api/categories/5
        public async Task Delete(int id)
        {
            await ForumServer.Instance.Categories.DeleteCategoryAsync(id);
        }
    }
}