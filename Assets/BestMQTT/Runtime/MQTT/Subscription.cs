using System;
using System.Collections.Generic;

using BestMQTT.Packets;

namespace BestMQTT
{
    public delegate void SubscriptionAcknowledgementDelegate(MQTTClient client, SubscriptionTopic topic, SubscribeAckReasonCodes reasonCode);
    public delegate void SubscriptionMessageDelegate(MQTTClient client, SubscriptionTopic topic, string topicName, ApplicationMessage message);

    public readonly struct TopicFilter
    {
        public string OriginalFilter { get => this._filter; }
        private readonly string _filter;

        public TopicFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentException("All Topic Filters MUST be at least one character long");

            // “sport/tennis#” is not valid
            // “sport/tennis/#/ranking” is not valid

            this._filter = filter;
        }

        public bool IsMatching(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
                throw new ArgumentException("Topic Names MUST be at least one character long!");

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901246
            //  A subscription to “#” will not receive any messages published to a topic beginning with a $
            //  A subscription to “+/monitor/Clients” will not receive any messages published to “$SYS/monitor/Clients”
            if (topicName[0] == '$' && (this._filter[0] == '#' || this._filter[0] == '+'))
                return false;

            int filterIdx = 0;
            int topicNameIdx = 0;
            for (; filterIdx < this._filter.Length && topicNameIdx < topicName.Length; filterIdx++)
            {
                char filterChr = this._filter[filterIdx];

                switch(filterChr)
                {
                    // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901244
                    // Multi-level wildcard
                    case '#':
                        return true;
                        //break;

                    // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901245
                    // Single-level wildcard
                    case '+':
                        // For example, “sport/tennis/+” matches 
                        //  “sport/tennis/player1” and
                        //  “sport/tennis/player2”,
                        //  but not “sport/tennis/player1/ranking”.
                        while (topicNameIdx < topicName.Length && topicName[topicNameIdx] != '/')
                        {
                            topicNameIdx++;
                        }
                        
                        break;

                    default:
                        if (filterChr != topicName[topicNameIdx])
                        {
                            // if a Client subscribes to “sport/tennis/player1/#”, it would receive messages published using these Topic Names:
                            //  “sport/tennis/player1”

                            // “sport/#” also matches the singular “sport”, since # includes the parent level.

                            return false;
                        }
                        topicNameIdx++;
                        break;
                }
            }

            // matched all
            if (topicNameIdx == topicName.Length && filterIdx == this._filter.Length)
                return true;
            else if (topicNameIdx == topicName.Length)
            {
                if (filterIdx > 0 && this._filter[filterIdx - 1] == '/' && this._filter[filterIdx] == '#')
                    return true;
                else if (filterIdx < this._filter.Length - 1 && this._filter[filterIdx] == '/' && this._filter[filterIdx + 1] == '#')
                    return true;
                else if (filterIdx < this._filter.Length && this._filter[filterIdx] == '+')
                    return true;
            }

            return false;
        }

        public override string ToString() => this.OriginalFilter;
    }

    public class SubscriptionTopic
    {
        public Subscription Subscription { get; internal set; }
        public TopicFilter Filter { get; private set; }

        internal SubscriptionAcknowledgementDelegate AcknowledgementCallback;
        internal SubscriptionMessageDelegate MessageCallback;

        public SubscriptionTopic(string topicFilter)
        {
            this.Filter = new TopicFilter(topicFilter);
        }

        public override string ToString() => $"[({this.Subscription.ID}){this.Filter}]";
    }

    public sealed class Subscription
    {
        internal MQTTClient Parent { get; private set; }
        internal UInt32 ID { get; private set; }
        public List<SubscriptionTopic> Topics { get => this._topics; }

        private List<SubscriptionTopic> _topics;

        internal Subscription(MQTTClient parent, UInt32 id)
        {
            this.Parent = parent;
            this.ID = id;
            this._topics = new List<SubscriptionTopic>();
        }

        internal void AddTopic(SubscriptionTopic topic)
        {
            topic.Subscription = this;
            this._topics.Add(topic);
        }

        public (bool topicFound, bool removeSubscription) TryRemoveTopic(string topicFilter)
        {
            int idx = -1;
            for (int i = 0; i < this.Topics.Count; ++i)
                if (this.Topics[i].Filter.OriginalFilter.Equals(topicFilter))
                {
                    idx = i;
                    break;
                }

            if (idx != -1)
                this.Topics.RemoveAt(idx);

            return (idx != -1, this.Topics.Count == 0);
        }

        public bool HasMatchingTopic(string topicName)
        {
            for (int i = 0; i < this._topics.Count; ++i)
                if (this._topics[i].Filter.IsMatching(topicName))
                    return true;

            return false;
        }
    }
}
