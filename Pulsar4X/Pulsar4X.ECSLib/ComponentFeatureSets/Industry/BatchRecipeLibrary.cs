using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class BatchRecipeLibrary
    {
        private List<BatchRecipe> _recipes;

        public BatchRecipeLibrary()
        {
            _recipes = new List<BatchRecipe>();
        }

        public void Add(BatchRecipe theRecipe)
        {
            if (_recipes.Any(tg => tg.Name == theRecipe.Name))
            {
                throw new Exception("There is already a batch recipe with the name " + theRecipe.Name + " in BatchRecipeLibrary.");
            }

            _recipes.Add(theRecipe);
        }

        public BatchRecipe Get(string recipeName)
        {
            if (_recipes.Any(tg => tg.Name == recipeName))
            {
                return _recipes.Single(tg => tg.Name == recipeName);
            }

            throw new Exception("Batch recipe with the name " + recipeName + " not found in BatchRecipeLibrary. Was the recipe properly loaded?");
        }
    }
}
