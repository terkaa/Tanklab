using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace BestMQTT.Examples.Helpers
{
    public class SubscriptionListItem : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private Text _text;
#pragma warning restore

        public GenericClient Parent { get; private set; }
        public TopicFilter Topic { get; private set; }
        public string Color { get; private set; }

        public void Set(GenericClient parent, string topic, string color)
        {
            this.Parent = parent;
            this.Topic = new TopicFilter(topic);
            this.Color = color;

            this._text.text = $"<color=#{color}>{topic}</color>";
        }

        public void AddLeftPadding(int padding)
        {
            this.GetComponent<LayoutGroup>().padding.left += padding;
        }

        public void OnUnsubscribeButton()
        {
            this.Parent.Unsubscribe(this.Topic.OriginalFilter);
        }
    }
}
