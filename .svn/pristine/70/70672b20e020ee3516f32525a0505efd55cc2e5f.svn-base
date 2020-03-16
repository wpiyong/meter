using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using System.Windows.Threading;

namespace gUV.View
{
    /// <summary>
    /// Interaction logic for NumericUpDownControl.xaml
    /// </summary>
    public partial class NumericUpDownControl : UserControl
    {
        #region Properties

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register("Prefix", typeof(string),
              typeof(NumericUpDownControl), new PropertyMetadata(""));

        public static readonly DependencyProperty PostfixProperty =
            DependencyProperty.Register("Postfix", typeof(string),
              typeof(NumericUpDownControl), new PropertyMetadata(""));

        public static readonly DependencyProperty ValueProperty = 
            DependencyProperty.Register("Value", 
            typeof(double), typeof(NumericUpDownControl), new PropertyMetadata(0D)); 
        
        public static readonly DependencyProperty MaxValueProperty = 
            DependencyProperty.Register("MaxValue", 
            typeof(double), typeof(NumericUpDownControl), new PropertyMetadata(100D)); 
        
        public static readonly DependencyProperty MinValueProperty = 
            DependencyProperty.Register("MinValue", 
            typeof(double), typeof(NumericUpDownControl), new PropertyMetadata(0D)); 
        
        public static readonly DependencyProperty IncrementProperty = 
            DependencyProperty.Register("Increment", 
            typeof(double), typeof(NumericUpDownControl), new PropertyMetadata(1D)); 
        
        public static readonly DependencyProperty LargeIncrementProperty = 
            DependencyProperty.Register("LargeIncrement", 
            typeof(double), typeof(NumericUpDownControl), new PropertyMetadata(5D));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register("ReadOnly",
            typeof(Boolean), typeof(NumericUpDownControl), new PropertyMetadata(true));

        
        public String Prefix
        {
            get { return (String)GetValue(PrefixProperty); }
            set { SetValue(PrefixProperty, value); }
        }
        public String Postfix
        {
            get { return (String)GetValue(PostfixProperty); }
            set { SetValue(PostfixProperty, value); }
        }
        public double Value 
        { 
            get { return (double)GetValue(ValueProperty); } 
            set { SetValue(ValueProperty, value); } 
        }
        public double MaxValue 
        { 
            get { return (double)GetValue(MaxValueProperty); } 
            set 
            { 
                SetValue(MaxValueProperty, value);
            } 
        }
        public double MinValue 
        {
            get { return (double)GetValue(MinValueProperty); } 
            set 
            { 
                SetValue(MinValueProperty, value);
            } 
        }
        public double Increment 
        {
            get { return (double)GetValue(IncrementProperty); } 
            set { SetValue(IncrementProperty, value); } 
        }
        public double LargeIncrement 
        {
            get { return (double)GetValue(LargeIncrementProperty); } 
            set { SetValue(LargeIncrementProperty, value); } 
        }
        public Boolean ReadOnly
        {
            get { return (Boolean)GetValue(ReadOnlyProperty); }
            set { SetValue(ReadOnlyProperty, value); }

        }
        #endregion


        private double _previousValue = 0;
        private DispatcherTimer _timer = new DispatcherTimer();
        private static int _delayRate = System.Windows.SystemParameters.KeyboardDelay; 
        private static int _repeatSpeed = Math.Max(1, System.Windows.SystemParameters.KeyboardSpeed);

        private bool _isIncrementing = false;

        public NumericUpDownControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;

            _textbox.PreviewTextInput += new TextCompositionEventHandler(_textbox_PreviewTextInput);
            _textbox.GotFocus += new RoutedEventHandler(_textbox_GotFocus); 
            _textbox.LostFocus += new RoutedEventHandler(_textbox_LostFocus);
            _textbox.PreviewKeyDown += new KeyEventHandler(_textbox_PreviewKeyDown);

            buttonIncrement.PreviewMouseLeftButtonDown += 
                new MouseButtonEventHandler(buttonIncrement_PreviewMouseLeftButtonDown); 
            buttonIncrement.PreviewMouseLeftButtonUp += 
                new MouseButtonEventHandler(buttonIncrement_PreviewMouseLeftButtonUp); 
            buttonDecrement.PreviewMouseLeftButtonDown += 
                new MouseButtonEventHandler(buttonDecrement_PreviewMouseLeftButtonDown); 
            buttonDecrement.PreviewMouseLeftButtonUp += 
                new MouseButtonEventHandler(buttonDecrement_PreviewMouseLeftButtonUp); 
            _timer.Tick += new EventHandler(_timer_Tick);
        }

        

        void _textbox_PreviewTextInput(object sender,                     
            TextCompositionEventArgs e) 
        {     
            if (!IsNumericInput(e.Text))     
            {         
                e.Handled = true;         
                return;
            } 
        }

        private bool IsNumericInput(string text) 
        { 
            foreach (char c in text) 
            { 
                if (!char.IsDigit(c) && c != '.') 
                { 
                    return false; 
                } 
            } 
            return true; 
        }

        

        void _textbox_GotFocus(object sender, RoutedEventArgs e) 
        { 
            _previousValue = Value; 
        }       
        
        void _textbox_LostFocus(object sender, RoutedEventArgs e) 
        { 
            double newValue = 0; 
            if (Double.TryParse(_textbox.Text, out newValue)) 
            { 
                if (newValue > MaxValue) 
                { 
                    newValue = MaxValue; 
                } 
                else if (newValue < MinValue) 
                { 
                    newValue = MinValue; 
                } 
            } 
            else 
            { 
                newValue = _previousValue; 
            } 
            _textbox.Text = string.Format("{0:N2}", newValue); 
        }

        void _textbox_PreviewKeyDown(object sender, KeyEventArgs e) 
        {     
            switch (e.Key)     
            {         
                case Key.Up:             
                    IncrementValue();             
                    break;         
                case Key.Down:             
                    DecrementValue();             
                    break;         
                case Key.PageUp:
                    LargeIncrementValue();             
                    break;         
                case Key.PageDown:
                    LargeDecrementValue();            
                    break;         
                default:
                    break;     
            } 
        }   
        
        private void IncrementValue() 
        {     
            Value = Math.Min(Value + Increment, MaxValue); 
        }   
        
        private void DecrementValue() 
        {    
            Value = Math.Max(Value - Increment, MinValue); 
        }

        private void LargeIncrementValue()
        {
            Value = Math.Min(Value + LargeIncrement, MaxValue);
        }

        private void LargeDecrementValue()
        {
            Value = Math.Max(Value - LargeIncrement, MinValue);
        }

        void buttonIncrement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) 
        { 
            buttonIncrement.CaptureMouse(); 
            _timer.Interval = TimeSpan.FromMilliseconds(_delayRate * 250); 
            _timer.Start(); 
            _isIncrementing = true; 
        }   
        
        void buttonIncrement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) 
        { 
            _timer.Stop(); 
            buttonIncrement.ReleaseMouseCapture(); 
            IncrementValue(); 
        }   
        
        void buttonDecrement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) 
        { 
            buttonDecrement.CaptureMouse(); 
            _timer.Interval = TimeSpan.FromMilliseconds(_delayRate * 250); 
            _timer.Start(); 
            _isIncrementing = false; 
        }  
        
        void buttonDecrement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) 
        { 
            _timer.Stop(); 
            buttonDecrement.ReleaseMouseCapture(); 
            DecrementValue(); 
        }   
        
        void _timer_Tick(object sender, EventArgs e) 
        { 
            if (_isIncrementing) 
            { 
                LargeIncrementValue(); 
            } 
            else 
            { 
                LargeDecrementValue();
            } 
            _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / _repeatSpeed); 
        }
    }


    public class ValueRangeRule : ValidationRule
    {
        public double Min { get; set; }
        public double Max { get; set; }
        
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double val = 0;
            double max = 0;
            double min = 0;

            try
            {
                var vm = ((BindingExpression)value).DataItem as NumericUpDownControl;
                max = vm.MaxValue;
                min = vm.MinValue;
                val = ((NumericUpDownControl)(((BindingExpression)value).ResolvedSource)).Value;
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((val < min) || (val > max))
            {
                return new ValidationResult(false,
                  "Please enter a value in the range: " + min + " : " + max + ".");
            }
            else
            {
                return new ValidationResult(true, null);
            }

        }

    }

}
