﻿namespace ServiceInsight.MessageViewers
{
    using System;
    using Framework.Rx;
    using Pirac;
    using ServiceInsight.Models;

    public abstract class BaseMessageViewModel : RxScreen, IDisplayMessageBody
    {
        IMessageView messageView;

        public BaseMessageViewModel(string displayName)
        {
            DisplayName = displayName;
            this.WhenPropertyChanged(nameof(SelectedMessage))
                .ObserveOnPiracMain()
                .Subscribe(_ => UpdateSelectedMessage());
        }

        public MessageBody SelectedMessage { get; set; }

        public void Display(StoredMessage selectedMessage)
        {
            if (SelectedMessage == selectedMessage) //Workaround, to force refresh the property. Should refactor to use the same approach as hex viewer.
            {
                UpdateSelectedMessage();
            }
            else
            {
                SelectedMessage = selectedMessage;
            }
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            messageView = (IMessageView)view;
            UpdateSelectedMessage();
        }

        void UpdateSelectedMessage()
        {
            if (messageView == null)
            {
                return;
            }

            messageView.Clear();

            if (SelectedMessage == null || SelectedMessage.Body == null)
            {
                return;
            }

            messageView.Display(SelectedMessage.Body.Text);
        }
    }
}