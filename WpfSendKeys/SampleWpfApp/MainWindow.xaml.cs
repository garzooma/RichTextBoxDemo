using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input.Test;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SampleWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            //KeyDown += MainWindow_KeyDown;
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                //textbox1.Clear();
            }
        }

        void SendToUIThread(UIElement element, string text)
        {
            element.Dispatcher.BeginInvoke(new Action(() =>
            {
                SendKeys.Send(element, text);
            }), DispatcherPriority.Input);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // run this on a background thread to not block the main window's message loop
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // post from the background thread to the UI thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    textbox1.Focus();
                }), DispatcherPriority.Input);

                //SendToUIThread(textbox1, "Hello World!");

                //// let the background thread sleep a little to let the UI display the text
                //// and to let the user see it
                //Thread.Sleep(2000);

                //// now send Ctrl+Z <-- this is what you couldn't mock before
                //SendToUIThread(this, "^z");

                //Thread.Sleep(2000);

                //// again post this on the UI thread to send the click to the button
                //SendToUIThread(textbox1, "{TAB}");
                //SendToUIThread(OKButton, "{ENTER}");
            });
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    public bool DelayTest { get; set; }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
      ThreadPool.QueueUserWorkItem(_ =>
      {
        // post from the background thread to the UI thread
        Dispatcher.BeginInvoke(new Action(() =>
        {
          textbox1.Focus();
        }), DispatcherPriority.Input);
        SendToUIThread(textbox1, "e");
        if (DelayTest) Thread.Sleep(10 * 100);
        SendToUIThread(textbox1, "{ENTER}");
      });
    }

    Action delayAction;

    private void PreviewKeyDownHandler(object sender, KeyEventArgs e)
    {
      Console.WriteLine("Key down: {0}; position: {1}", e.Key, CurrentPosition);
      if (movingCursor)
      {
        Console.WriteLine("Cursor hasn't moved yet");
        //MoveCursor();
        delayAction = new Action(() =>
        { PreviewKeyDownHandler(this, e); });
        return;
      }
      if (e.Key == Key.Enter) {
        string text = GetPrecedingText();
        if (abbreviations.ContainsKey(text.ToUpper()))
        {
          ReplaceText(text, abbreviations[text.ToUpper()]);
        }
        if (!text.EndsWith("."))
        {
          InsertText(".");
        }
      }

    }

    private Dictionary<string, string> abbreviations = new Dictionary<string, string>()
    {
      {"bae".ToUpper(), "biatrial enlargment" }
    };

    private bool movingCursor;

    private void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
    {
      Console.WriteLine("Text input: {0}; position: {1}", e.Text, CurrentPosition);
      movingCursor = true;

    }

    private bool performingDelayAction;

    private void SelectionChangedEvent(object sender, RoutedEventArgs e)
    {
      Console.WriteLine("Selection changed: position: {0}", CurrentPosition);
      movingCursor = false;
      if (delayAction != null && !performingDelayAction)
      {
        performingDelayAction = true;
        delayAction();
        delayAction = null;
        performingDelayAction = false;
      }

    }

    private int CurrentPosition
    {
      get
      {
        int ret = 0;
        TextBox textBox = textBoxBase as TextBox; 
        if (textBox != null)
        {
          ret = textBox.CaretIndex;
        }
        else if (textBoxBase is RichTextBox) {
          RichTextBox richTextBox = textBoxBase as RichTextBox;
          TextPointer pointer = richTextBox.CaretPosition;
          ret = richTextBox.Document.ContentStart.GetOffsetToPosition(pointer);
        }
        return ret;
      }
    }

    private string GetPrecedingText()
    {
      string text = string.Empty;
      TextBox textBox = textBoxBase as TextBox;
      if (textBox != null)
      {
              text = textBox.Text.Substring(0, textBox.CaretIndex);
      }
      else if (textBoxBase is RichTextBox)
      {
        RichTextBox richTextBox = textBoxBase as RichTextBox;
        TextPointer pointer = richTextBox.CaretPosition;
        text = pointer.GetTextInRun(LogicalDirection.Backward);
      }
      char[] delimiters = { ' ', '\n', ',' };
      string[] words = text.Split(delimiters);
      return words.LastOrDefault();
    }

    private void InsertText(string text)
    {

      TextBox textBox = textBoxBase as TextBox;
      if (textBox != null)
      {
        textBox.Text += text;
        textBox.CaretIndex = textBox.Text.Length;
      }
      else if (textBoxBase is RichTextBox)
      {
        RichTextBox richTextBox = textBoxBase as RichTextBox;
        if (richTextBox.CaretPosition.LogicalDirection == LogicalDirection.Backward)
        {
          richTextBox.CaretPosition = richTextBox.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
        }
        TextPointer pointer = richTextBox.CaretPosition;
        pointer.InsertTextInRun(text);
      }
      return;
    }

    private void ReplaceText(string text, string replaceText)
    {
      TextBox textBox = textBoxBase as TextBox;
      if (textBox != null)
      {
        StringBuilder sb = new StringBuilder(text);
        int removeStart = textBox.CaretIndex - text.Length;
        sb.Remove(removeStart, text.Length);
        sb.Insert(removeStart, replaceText);
        textBox.Text = sb.ToString();
        textBox.CaretIndex = removeStart + replaceText.Length;
      }
      else if (textBoxBase is RichTextBox)
      {
        RichTextBox richTextBox = textBoxBase as RichTextBox;
        if (richTextBox.CaretPosition.LogicalDirection == LogicalDirection.Backward)
        {
          richTextBox.CaretPosition = richTextBox.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
        }
        TextPointer removePointer = richTextBox.CaretPosition.GetPositionAtOffset(-text.Length);
        removePointer.DeleteTextInRun(text.Length);
        removePointer.InsertTextInRun(replaceText);
        richTextBox.CaretPosition = removePointer;
      }
      return;
    }

    private void MoveCursor()
    {
      TextBox textBox = textBoxBase as TextBox;
      if (textBox != null)
      {
        textBox.CaretIndex += 1;
      }
      else if (textBoxBase is RichTextBox)
      {
        RichTextBox richTextBox = textBoxBase as RichTextBox;
        if (richTextBox.CaretPosition.LogicalDirection == LogicalDirection.Backward)
        {
          richTextBox.CaretPosition = richTextBox.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
        }
        TextPointer newPosition = richTextBox.CaretPosition.GetPositionAtOffset(1);
        if (newPosition != null) richTextBox.CaretPosition = newPosition;
      }
      return;
    }

    private TextBoxBase textBoxBase
    {
      get { 
        return textbox1 as TextBoxBase;
      }
    }
  }
}
