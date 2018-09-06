using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ObservableBinding
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static BehaviorSubject<T> Behavior<T>(T initialValue)
        {
            return new BehaviorSubject<T>(initialValue);
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = DesignData;
        }

        public static object DesignData = new
        {
            Text = Behavior("asdf"),
            Items = new[]
            {
                new { Text = Behavior("qwer") }
            },
            Child1 = new
            {
                Text = Behavior("child1 text")
            },
            Child2 = new
            {
                Text = Behavior("child1 text")
            }
        };
    }
}
