// <copyright company="SIX Networks GmbH" file="BuiltInContentContainer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Glue.Helpers;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public abstract class BuiltInContainer<T> : SelectionList<T> where T : class
    {
        string _name;

        protected BuiltInContainer(string name) {
            Name = name;
        }

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }
    }

    public class BuiltInContentContainer : BuiltInContainer<IContent>
    {
        public BuiltInContentContainer(string name) : base(name) {}
    }
}