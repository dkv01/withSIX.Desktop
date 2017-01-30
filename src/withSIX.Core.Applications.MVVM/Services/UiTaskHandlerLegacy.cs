// <copyright company="SIX Networks GmbH" file="UiTaskHandlerLegacy.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ReactiveUI;
using withSIX.Core.Applications.MVVM.Extensions;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;

namespace withSIX.Core.Applications.MVVM.Services
{
    public static class UiTaskHandlerExtensions
    {
        [Obsolete("Use RXUI or custom factory methods instead")]
        public static ReactiveCommand<TOut> SetNewCommand<T, TOut>(this ReactiveCommand<TOut> command, T target,
            Expression<Func<T, ReactiveCommand<TOut>>> memberLamda)
            where T : class {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (memberLamda == null) throw new ArgumentNullException(nameof(memberLamda));

            var memberSelectorExpression = memberLamda.Body as MemberExpression;
            if (memberSelectorExpression == null)
                throw new Exception("Not a member expression? " + target);
            var property = memberSelectorExpression.Member as PropertyInfo;
            if (property == null)
                throw new Exception("Not a property? " + target);
            command.DefaultSetup(target.GetCallerName(property));
            property.SetValue(target, command, null);
            return command;
        }

        [Obsolete("Use RXUI or custom factory methods instead")]
        public static ReactiveCommand SetCommand<T>(this T target, Expression<Func<T, ReactiveCommand>> memberLamda)
            where T : class {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (memberLamda == null) throw new ArgumentNullException(nameof(memberLamda));

            var memberSelectorExpression = memberLamda.Body as MemberExpression;
            if (memberSelectorExpression == null)
                throw new Exception("Not a member expression? " + target);
            var property = memberSelectorExpression.Member as PropertyInfo;
            if (property == null)
                throw new Exception("Not a property? " + target);
            var reactiveCommand = CreateCommand(GetCallerName(target, property));
            property.SetValue(target, reactiveCommand, null);
            return reactiveCommand;
        }

        [Obsolete("Use RXUI or custom factory methods instead")]
        public static ReactiveCommand SetCommand<T>(this T target, Expression<Func<T, ReactiveCommand>> memberLamda,
            IObservable<bool> ena,
            bool initialCondition = true) where T : class {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (memberLamda == null) throw new ArgumentNullException(nameof(memberLamda));
            if (ena == null) throw new ArgumentNullException(nameof(ena));

            var memberSelectorExpression = memberLamda.Body as MemberExpression;
            if (memberSelectorExpression == null)
                throw new Exception("Not a member expression? " + target);
            var property = memberSelectorExpression.Member as PropertyInfo;
            if (property == null)
                throw new Exception("Not a property? " + target);
            var reactiveCommand = CreateCommand(ena, GetCallerName(target, property), initialCondition);
            property.SetValue(target, reactiveCommand, null);
            return reactiveCommand;
        }

        static string GetCallerName<T>(this T target, PropertyInfo property)
            => target.GetType().Name + "." + property.Name;

        public static ReactiveCommand ToCommand(this Action action, [CallerMemberName] string name = null) {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (name == null) throw new ArgumentNullException(nameof(name));
            var command = CreateCommand(name);
            command.Subscribe(action);
            return command;
        }

        public static ReactiveCommand ToAsyncCommand(this Func<Task> action, string name) {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (name == null) throw new ArgumentNullException(nameof(name));
            var command = CreateCommand(name);
            command.RegisterAsyncTask(action).Subscribe();
            return command;
        }

        static ReactiveCommand CreateCommand(string name = null) {
            var command = new ReactiveCommand();
            command.DefaultSetup(name);
            return command;
        }

        static ReactiveCommand CreateCommand(IObservable<bool> ena, string name, bool initialCondition = true) {
            if (ena == null) throw new ArgumentNullException(nameof(ena));
            if (name == null) throw new ArgumentNullException(nameof(name));

            var command = new ReactiveCommand(ena, initialCondition);
            command.DefaultSetup(name);
            return command;
        }

        public static ReactiveCommand CreateCommand(string name, bool allowMultiple, bool initialCondition = true) {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var command = new ReactiveCommand(null, allowMultiple, null, initialCondition);
            command.DefaultSetup(name);
            return command;
        }
    }
}