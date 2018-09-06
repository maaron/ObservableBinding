using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

using System.Reactive.Linq;
using System.Threading;
using System.Reflection;

namespace ObservableBinding
{
    [MarkupExtensionReturnType(typeof(object))]
    public class ObservableBindingExtension : MarkupExtension
    {
        public static List<ObservableBindingExtension> Instances = new List<ObservableBindingExtension>();

        private Dictionary<DependencyObject, IList<IDisposable>> DependencyObjectSubscriptions = 
            new Dictionary<DependencyObject, IList<IDisposable>>();

        [ConstructorArgument("Path")]
        public PropertyPath Path { get; set; }

        public ObservableBindingExtension()
        {
            Instances.Add(this);
        }

        public ObservableBindingExtension(PropertyPath path)
        {
            Path = path;
            Instances.Add(this);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var targetProvider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

            if (targetProvider.TargetObject is DependencyObject)
            {
                var dependencyObject = (DependencyObject)targetProvider.TargetObject;
                var subscriptions = new List<IDisposable>();
                DependencyObjectSubscriptions.Add(dependencyObject, subscriptions);

                if (targetProvider.TargetProperty is DependencyProperty)
                {
                    var dependencyProperty = (DependencyProperty)targetProvider.TargetProperty;

                    var frameworkElement = GetFrameworkElement(dependencyObject);
                    if (frameworkElement != null)
                    {
                        subscriptions.Add(SetupBinding(frameworkElement, dependencyObject, dependencyProperty));

                        frameworkElement.DataContextChanged += (s, e) =>
                        {
                            foreach (var sub in subscriptions) sub.Dispose();
                            subscriptions.Clear();

                            if (e.NewValue != null)
                                SetupBinding(frameworkElement, dependencyObject, dependencyProperty);
                        };

                        frameworkElement.Unloaded += (s, e) =>
                        {
                            foreach (var sub in subscriptions)
                                sub.Dispose();
                        };
                    }
                }
            }

            return this;
        }

        public static FrameworkElement GetFrameworkElement(DependencyObject d)
        {
            while (!(d is FrameworkElement) && d != null)
                d = LogicalTreeHelper.GetParent(d);

            return (d as FrameworkElement);
        }

        private IDisposable SetupBinding(FrameworkElement frameworkElement, DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            var observable = ResolvePath(frameworkElement, Path);

            var observableGenericType = 
                observable.GetType()
                .GetInterfaces()
                .Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == (typeof(IObservable<>)))
                .GetGenericArguments()[0];

            MethodInfo method = 
                typeof(ObservableBindingExtension)
                .GetMethod(nameof(SubscribeObservable), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(observableGenericType);

            return (IDisposable)method.Invoke(
                this, new object[] { observable, dependencyObject, dependencyProperty });

        }

        private IDisposable SubscribeObservable<T>(
            IObservable<T> observable, 
            DependencyObject dependencyObject, 
            DependencyProperty dependencyProperty)
        {
            return observable
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(value => dependencyObject.SetValue(dependencyProperty, value));
        }

        public static object ResolvePath(FrameworkElement frameworkElement, PropertyPath path)
        {
            var properties = path.Path.Split(',');
            var current = frameworkElement.DataContext;

            foreach (var prop in properties)
            {
                if (current == null) break;
                current = current.GetType().GetProperty(prop).GetValue(current);
            }

            return current;
        }
    }

    public class ObservableViewModel<T> : INotifyPropertyChanged, IDisposable
    {
        public string PropertyName { get; }
        public IObservable<T> Observable { get; }
        private IDisposable Subscription { get; }

        public ObservableViewModel(IObservable<T> observable, string propertyName)
        {
            PropertyName = propertyName;
            Observable = observable;
            Subscription = observable.Subscribe(value =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            Subscription.Dispose();
        }
    }

    [MarkupExtensionReturnType(typeof(Brush))]
    public class StyleBindingExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}
