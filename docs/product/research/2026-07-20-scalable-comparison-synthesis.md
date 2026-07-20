# Scalable Comparison Research Synthesis

Date: 2026-07-20

## Decision

SoundLens should retain A/B comparison as its smallest trustworthy evidence and report view. At campaign scale, A/B becomes the drill-down destination beneath metadata-driven reference-to-many, matched-pair, cohort, condition-matrix, and exception-triage workflows.

This is a roadmap direction, not a claim that the current temporary-session architecture supports production-scale campaigns.

## Professional-Tool Patterns

- Siemens Simcenter Testlab organizes NVH campaigns around centrally stored, annotated test data that can be searched, retrieved, compared, processed, and reported. This supports a metadata-first campaign model rather than a browser list of opened files. [Simcenter Testlab Data Management](https://www.siemens.com/en-us/products/simcenter/simulation-test/test-data-management/)
- ASAM ODS defines a vendor-neutral test-data-management foundation spanning measurement, simulation, calibration, workflows, instrumentation, and large-data connectors. This supports explicit project, test, measurement, channel, and result identities. [ASAM ODS](https://www.asam.net/standards/detail/ods/)
- HBK BK Connect applies sequenced or parallel processing chains to multiple imported datasets, while Team Server indexes metadata and supports previewing before selected data is loaded. This supports versioned recipes, bounded batch execution, metadata search, and lazy evidence retrieval. [BK Connect Data Processing](https://www.hbkworld.com/en/products/software/daq/bk-connect-pulse/core/data-processing/data-processing-software) and [BK Connect Team Server](https://www.bksv.com/en/analysis-software/data-acquisition-software/bk-connect/data-management/team-server)
- HEAD acoustics ArtemiS SUITE separates sources, filters, analyses, statistics, and destinations, and its batch tooling applies template-derived processing to sets of compatible recordings. This supports reusable processing graphs and explicit compatibility gates rather than silent all-to-all comparison. [ArtemiS SUITE Pool Project](https://cdn.head-acoustics.com/fileadmin/data/en/Data-Sheets/AS/ASM_15/D50010e-APR-010-ArtemiS-SUITE-Pool-Project.pdf) and [Batch RPM Generator](https://cdn.head-acoustics.com/fileadmin/data/en/Data-Sheets/AS/ASM_15/D51304e-ASP-304-ArtemiS-SUITE-Batch-RPM-Generator.pdf)

## SoundLens Implications

1. Model the campaign and its conditions before adding a broad batch dashboard.
2. Start scale workflows with reference-to-many and explicit metadata-matched pairs; they extend the current A/B contract with less semantic risk than arbitrary cohorts.
3. Preserve separate units, coverage, limitations, exclusions, unmatched items, and per-item failures.
4. Present large result sets through filtering, exact-value tables, distributions, metric-specific matrices, and exception queues.
5. Drill every aggregate result into a bounded focused or A/B evidence view.
6. Keep raw files, searchable metadata, and versioned derived results logically separate; introduce physical infrastructure only when a deployment requires it.
7. Let Copilot plan and explain batches, but keep selection resolution, alignment, computation, aggregation, and provenance backend-owned.

## Discovery Questions

The phrase "thousands of signals" must be resolved during interviews. It may refer to physical sensor channels, long sample arrays, derived curves, operating-point slices, repeated events, or campaign-wide totals. Capture typical and worst-case counts for each category, along with matching metadata, latency expectations, failure tolerance, and the decision users make from the aggregate view.

Do not set a production scale claim until representative customer campaigns are available. The existing 100-recording and 10,000-summary benchmark remains an intermediate engineering gate, not a market requirement.
