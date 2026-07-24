For every session, a unique chat page is created. A chat page holds a reference to the session and for every message, the session returns the ui element that should be displayed next in the scroll view.

The prompt field takes the prompt and controls the model, it's settings and displays the files and images in the context.

For every type of message, a different kind of ui control is displayed. System messages are not displyed.
This is done to avoid complex converter logic in the xaml and code behind.
