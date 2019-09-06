using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.Test;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RichTextBoxDemo
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    private Key[] delimiterKeys = { Key.Return, Key.Space };

    private Action delayAction;

    public bool DelayActionFacility { get; set; }

    public bool DelayTest { get; set; }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
      ThreadPool.QueueUserWorkItem(_ => {
        // post from the background thread to the UI thread
        Dispatcher.BeginInvoke(new Action(() => {
          textbox1.Focus();
        }), DispatcherPriority.Input);
        SendToUIThread(textbox1, "e");
        if (DelayTest) Thread.Sleep(10 * 100);
        SendToUIThread(textbox1, "{ENTER}");
      });
    }

    void SendToUIThread(UIElement element, string text)
    {
      element.Dispatcher.BeginInvoke(new Action(() => {
        SendKeys.Send(element, text);
      }), DispatcherPriority.Input);
    }

    private bool changingText;

    private void PreviewKeyDownHandler(object sender, KeyEventArgs e)
    {
      if (DelayActionFacility && changingText)
      {
        if (delayAction == null)
        {
          delayAction = new Action(() => { PreviewKeyDownHandler(this, e); });
        }
        return;
      }
      if (delimiterKeys.Contains(e.Key))
      {
        string text = GetPrecedingText();
        if (abbreviations.ContainsKey(text.ToUpper()))
        {
          TextPointer pointer = textbox1.CaretPosition.GetPositionAtOffset(-text.Length);
          if (pointer.LogicalDirection == LogicalDirection.Backward)
          {
            pointer = pointer.GetPositionAtOffset(0, LogicalDirection.Forward);
            textbox1.CaretPosition = pointer;
          }
          pointer.DeleteTextInRun(text.Length);
          pointer.InsertTextInRun(abbreviations[text.ToUpper()]);
        }
        if (e.Key == Key.Enter && !text.EndsWith("."))
        {
          TextPointer pointer = textbox1.CaretPosition;
          if (pointer.LogicalDirection == LogicalDirection.Backward)
          {
            pointer = pointer.GetPositionAtOffset(0, LogicalDirection.Forward);
            textbox1.CaretPosition = pointer;
          }
          pointer.InsertTextInRun(".");
        }
      }
    }

    private Dictionary<string, string> abbreviations = new Dictionary<string, string>()
    {
      {"BAE", "biaterial enlargement" },
    };

    private char[] delimiters = { ' ', '\n', ',' };

    private string GetPrecedingText()
    {
      string text = string.Empty;
      TextPointer pointer = textbox1.CaretPosition;
      text = pointer.GetTextInRun(LogicalDirection.Backward);
      string[] words = text.Split(delimiters);
      return words.LastOrDefault();
    }

    private void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
    {
      changingText = true;
    }

    private void TextChangedHandler(object sender, TextChangedEventArgs e)
    {
      changingText = false;
      if (delayAction != null)
      {
        Action action = delayAction;
        delayAction = null;
        action();
      }
    }
  }
}
