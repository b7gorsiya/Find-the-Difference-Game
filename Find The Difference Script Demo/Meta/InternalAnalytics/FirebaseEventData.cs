using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

namespace CrimsonGames.Analytics
{
    public static class FirebaseParameters
    {
        public const string FirebaseEventDataPhylumParam = "phylum";
        public const string FirebaseEventDataFamilyParam = "family";
        public const string FirebaseEventDataGenusParam = "genus";
        public const string FirebaseEventDataSpeciesParam = "species";
    }

    [Serializable]
    public class FirebaseEventData
    {

        public string EventName { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }

        public FirebaseEventData(string eventName)
        {
            EventName = eventName;
            Parameters = new Dictionary<string, object>();
        }

        public void AddParameter(string key, object value)
        {
            if (value is int)
                Parameters[key] = (int)value;
            else if (value is float)
                Parameters[key] = (float)value;
            else if (value is bool)
                Parameters[key] = (bool)value ? 1 : 0; // Convert bool to Firebase int
            else if (value is long)
                Parameters[key] = (long)value;
            else
                Parameters[key] = value.ToString(); // Default to string
        }

        public void SendToFirebase()
        {
            Debug.Log("[FirebaseEventData](SendToFirebase) called");
            Parameter[] firebaseParams = new Parameter[Parameters.Count];
            int index = 0;

            foreach (var param in Parameters)
            {
                if (param.Value is int intValue)
                    firebaseParams[index++] = new Parameter(param.Key, intValue);
                else if (param.Value is float floatValue)
                    firebaseParams[index++] = new Parameter(param.Key, floatValue);
                else if (param.Value is long longValue)
                    firebaseParams[index++] = new Parameter(param.Key, longValue);
                else
                    firebaseParams[index++] = new Parameter(param.Key, param.Value.ToString()); // Keep string as string
            }

            Debug.Log($"[FirebaseEventData](SendToFirebase) pre send, event name :: {EventName}, parameter count :: {firebaseParams.Length}");
            FirebaseAnalytics.LogEvent(EventName, firebaseParams);
            Debug.Log($"Sent event: {EventName} with {Parameters.Count} parameters.");
        }
    }
}