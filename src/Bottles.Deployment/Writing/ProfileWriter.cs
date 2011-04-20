using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using FubuCore;
using FubuCore.Reflection;
using FubuCore.Util;

namespace Bottles.Deployment.Writing
{
    public class ProfileWriter
    {
        private readonly string _destination;
        private readonly IFileSystem _system;
        private readonly Cache<string, RecipeDefinition> _recipes = new Cache<string, RecipeDefinition>(name => new RecipeDefinition(name));
        private readonly IList<PropertyValue> _profileValues = new List<PropertyValue>();
        private readonly TypeDescriptorCache _types = new TypeDescriptorCache();


        public ProfileWriter(string destination) : this(destination, new FileSystem())
        {
        }

        public ProfileWriter(string destination, IFileSystem system)
        {
            _destination = destination;
            _system = system;
        }

        public RecipeDefinition RecipeFor(string name)
        {
            return _recipes[name];
        }

        public void Flush()
        {
            _system.DeleteDirectory(_destination);

            _system.CreateDirectory(_destination);
            _system.CreateDirectory(FileSystem.Combine(_destination, ProfileFiles.RecipesFolder));

            writeEnvironmentSettings();

            _recipes.Each(writeRecipe);
        }

        private void writeEnvironmentSettings()
        {
            var writer = new StringWriter();
            _profileValues.Each(v => writer.WriteLine(v.ToString()));

            var environmentFile = FileSystem.Combine(_destination, ProfileFiles.EnvironmentSettingsFileName);
            _system.WriteStringToFile(environmentFile, writer.ToString());
        }

        private void writeRecipe(RecipeDefinition recipe)
        {
            var recipeDirectory = FileSystem.Combine(_destination, ProfileFiles.RecipesFolder, recipe.Name);
            _system.CreateDirectory(recipeDirectory);

            var controlFilePath = FileSystem.Combine(recipeDirectory, ProfileFiles.RecipesControlFile);
            _system.WriteStringToFile(controlFilePath,"");
            recipe.Dependencies.Each(d =>
                {
                    var line = "Dependency:{0}\n".ToFormat(d);
                    _system.AppendStringToFile(controlFilePath, line);
                });

            recipe.Hosts().Each(host => writeHost(host, recipeDirectory));
        }

        private void writeHost(HostDefinition host, string recipeDirectory)
        {
            new HostWriter(_types).WriteTo(host, recipeDirectory);
        }


        public void AddEnvironmentSetting<T>(Expression<Func<T, object>> property, string host, object value)
        {
            _profileValues.Add(new PropertyValue(){
                Accessor = property.ToAccessor(),
                HostName = host,
                Value = value
            });
        }

        public void AddEnvironmentSetting(string name, object value)
        {
            _profileValues.Add(new PropertyValue(){
                Name = name,
                Value = value
            });
        }


    }
}