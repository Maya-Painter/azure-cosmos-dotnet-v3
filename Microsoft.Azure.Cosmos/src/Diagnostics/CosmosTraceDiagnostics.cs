﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Azure.Cosmos.Json;
    using Microsoft.Azure.Cosmos.Query.Core.Metrics;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Cosmos.Tracing.TraceData;
    using static Microsoft.Azure.Cosmos.Tracing.TraceData.ClientSideRequestStatisticsTraceDatum;

    internal sealed class CosmosTraceDiagnostics : CosmosDiagnostics
    {
        private ServerSideAccumulatedMetrics cachedServerSideAccumulatedMetrics { get; set; }
        public CosmosTraceDiagnostics(ITrace trace)
        {
            if (trace == null)
            {
                throw new ArgumentNullException(nameof(trace));
            }

            // Need to set to the root trace, since we don't know which layer of the stack the response message was returned from.
            ITrace rootTrace = trace;
            while (rootTrace.Parent != null)
            {
                rootTrace = rootTrace.Parent;
            }

            this.Value = rootTrace;
            this.cachedServerSideAccumulatedMetrics = null;
        }

        public ITrace Value { get; }

        public override string ToString()
        {
            return this.ToJsonString();
        }

        public override TimeSpan GetClientElapsedTime()
        {
            return this.Value.Duration;
        }

        public override IReadOnlyList<(string regionName, Uri uri)> GetContactedRegions()
        {
            return this.Value?.Summary?.RegionsContacted;
        }

        public override ServerSideAccumulatedMetrics GetQueryMetrics()
        {
            if (this.cachedServerSideAccumulatedMetrics != null)
            {
                return this.cachedServerSideAccumulatedMetrics;
            }

            ServerSideMetricsAccumulator accumulator = new ServerSideMetricsAccumulator();
            ServerSideMetricsAccumulator.WalkTraceTreeForQueryMetrics(this.Value, accumulator);

            List<PartitionedServerSideMetrics> partitionedServerSideMetrics = 
                accumulator.GetPartitionedServerSideMetrics().Select(metrics => new PartitionedServerSideMetrics(metrics)).ToList();

            if (partitionedServerSideMetrics.Count == 0)
            {
                return null;
            }

            ServerSideMetrics serverSideMetrics = new ServerSideMetrics(accumulator.GetServerSideMetrics());
            this.cachedServerSideAccumulatedMetrics = new ServerSideAccumulatedMetrics(serverSideMetrics, partitionedServerSideMetrics);
            return this.cachedServerSideAccumulatedMetrics;
        }

        internal bool IsGoneExceptionHit()
        {
            return this.WalkTraceTreeForGoneException(this.Value);
        }

        private bool WalkTraceTreeForGoneException(ITrace currentTrace)
        {
            if (currentTrace == null)
            {
                return false;
            }

            foreach (object datum in currentTrace.Data.Values)
            {
                if (datum is ClientSideRequestStatisticsTraceDatum clientSideRequestStatisticsTraceDatum)
                {
                    foreach (StoreResponseStatistics responseStatistics in clientSideRequestStatisticsTraceDatum.StoreResponseStatisticsList)
                    {
                        if (responseStatistics.StoreResult != null && responseStatistics.StoreResult.StatusCode == Documents.StatusCodes.Gone)
                        {
                            return true;
                        }
                    }
                }
            }

            foreach (ITrace childTrace in currentTrace.Children)
            {
                if (this.WalkTraceTreeForGoneException(childTrace))
                {
                    return true;
                }
            }

            return false;
        }

        private string ToJsonString()
        {
            ReadOnlyMemory<byte> utf8String = this.WriteTraceToJsonWriter(JsonSerializationFormat.Text);
            return Encoding.UTF8.GetString(utf8String.Span);
        }

        private ReadOnlyMemory<byte> WriteTraceToJsonWriter(JsonSerializationFormat jsonSerializationFormat)
        {
            IJsonWriter jsonTextWriter = JsonWriter.Create(jsonSerializationFormat);
            TraceWriter.WriteTrace(jsonTextWriter, this.Value);
            return jsonTextWriter.GetResult();
        }

        public override DateTime? GetStartTimeUtc()
        {
            if (this.Value == null || this.Value.StartTime == null)
            {
                return null;
            }

            return this.Value.StartTime;
        }

        public override int GetFailedRequestCount()
        {
            if (this.Value == null || this.Value.Summary == null)
            {
                return 0;
            }

            return this.Value.Summary.GetFailedCount();
       }
    }
}