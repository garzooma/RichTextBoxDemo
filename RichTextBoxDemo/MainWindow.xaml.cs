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

    /// <summary>
    /// Keys which cause auto-correction
    /// </summary>
    private Key[] delimiterKeys = { Key.Return, Key.Space };

    /// <summary>
    /// Action to perform after current character has been fully
    /// processed.
    /// </summary>
    private Action delayAction;

    /// <summary>
    /// Flag to indicate we're using the delay action facility to
    /// delay handing of a key press until after preceding character
    /// has been processed.
    /// </summary>
    public bool DelayActionFacility { get; set; }

    /// <summary>
    /// See if we're delaying a 2nd keystroke in the Test_Click method by
    /// a certain amount of time.
    /// </summary>
    public bool DelayTest { get; set; }

    /// <summary>
    /// Perform test where 2 key strokes are sent one immediately after
    /// the other.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Test_Click(object sender, RoutedEventArgs e)
    {
      // Simulate key strokes
      ThreadPool.QueueUserWorkItem(_ => {
        // post from the background thread to the UI thread
        Dispatcher.BeginInvoke(new Action(() => {
          textbox1.Focus();
        }), DispatcherPriority.Input);
        SendToUIThread(textbox1, "k");
        if (DelayTest) Thread.Sleep(10 * 100);  // see about delaying 2nd keystroke
        SendToUIThread(textbox1, "{ENTER}");
      });
    }

    /// <summary>
    /// Simulate key stroke
    /// </summary>
    /// <param name="element"></param>
    /// <param name="text"></param>
    void SendToUIThread(UIElement element, string text)
    {
      element.Dispatcher.BeginInvoke(new Action(() => {
        SendKeys.Send(element, text);
      }), DispatcherPriority.Input);
    }

    private bool changingText;

    private void PreviewKeyDownHandler(object sender, KeyEventArgs e)
    {
      // Are we seeing a key press before preceding character is processed?
      if (DelayActionFacility && changingText)
      {
        if (delayAction == null)
        {
          // Not done processing previous character --
          // -- set to redo in TextChanged event.
          delayAction = new Action(() => { PreviewKeyDownHandler(this, e); });
        }
        return;
      }
      // See if we're at the end of a word
      if (delimiterKeys.Contains(e.Key))
      {
        string word = GetPrecedingText();
        // Is the word an abbreviation?
        if (abbreviations.ContainsKey(word.ToUpper()))
        {
          TextPointer pointer = textbox1.CaretPosition.GetPositionAtOffset(-word.Length);
          if (pointer.LogicalDirection == LogicalDirection.Backward)  // change direction, if needed
          {
            pointer = pointer.GetPositionAtOffset(0, LogicalDirection.Forward);
            textbox1.CaretPosition = pointer;
          }
          // replace abbreviation w/expansion
          pointer.DeleteTextInRun(word.Length);
          pointer.InsertTextInRun(abbreviations[word.ToUpper()]);
        }
        // Are we at the end of a line, and don't have a period?
        if (e.Key == Key.Enter && !word.EndsWith("."))
        {
          TextPointer pointer = textbox1.CaretPosition;
          if (pointer.LogicalDirection == LogicalDirection.Backward)  // change direction, if needed
          {
            pointer = pointer.GetPositionAtOffset(0, LogicalDirection.Forward);
            textbox1.CaretPosition = pointer;
          }
          pointer.InsertTextInRun(".");
        }
      }
    }

    /// <summary>
    /// List of abbreviations and expansions (only one for testing).
    /// </summary>
    private Dictionary<string, string> abbreviations = new Dictionary<string, string>()
    {
      {"IDK", "I don't know" },
    };

    /// <summary>
    /// Characters to check for delimiting words
    /// </summary>
    private char[] delimiters = { ' ', '\n', ',' };

    /// <summary>
    /// Get preceding word, separated by delimiter characters
    /// </summary>
    /// <returns></returns>
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
      changingText = false; // we're done processing a keystroke
      // do we have any auto-correct action to perform?
      if (delayAction != null)  
      {
        Action action = delayAction;
        delayAction = null;
        action(); // do the autocorrection from PreviewKeyDown event
      }
    }
  }
}
