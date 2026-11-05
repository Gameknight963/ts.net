# Working on ts.net

## Project direction

- Compile actual TypeScript using Microsoft’s compiler frontend.
- Write the compiler in Crust. Use the existing C# assembly writer where needed.
- Expand TypeScript support while improving performance.
- Aim to beat V8 substantially, but support performance claims with measurements.
- When an approach stalls, inspect the generated IL and machine code, compare
  equivalent C#, and investigate what V8 does.
- Keep trying promising approaches. If repeated attempts indicate that the
  approach needs reconsideration, explain the evidence.

## Implementation

- Preserve TypeScript behavior when optimizing. Use proofs or runtime guards
  with a correct fallback.
- Prefer safe Crust code where practical. Keep raw pointers and unsafe code
  where they make sense.
- Observe Crust’s strengths, annoyances, and architectural issues while working.
  Put these observations under artifacts; do not commit them.
- Do not introduce dependencies with licensing obligations without discussing
  those obligations first.

## Validation and benchmarks

- Test supported behavior against Node.
- Compare equivalent workloads and verify matching results.
- Use Release builds, warmup, repeated measurements, and fresh processes.
- Distinguish execution time from startup and compilation time.
- Inspect generated code or allocation measurements when explaining an
  optimization. Do not assume the JIT removes something merely because it could.
- Keep experiments and raw benchmark results under artifacts.
- State the limits of the evidence. One microbenchmark does not establish
  overall application performance.

Good:
“This loop improved from 5.3 ms to 1.6 ms and no longer allocates.
Node measured 0.68 ms on the same workload.”

Bad:
“The runtime is now three times faster.”

The good example identifies the workload and comparison. The bad example
generalizes a specific result to the whole runtime.

## README and documentation

Occasionally update the README’s Current Performance section when new
measurements make it more useful, especially after meaningful improvements
or the addition of representative workloads. Routine fluctuations and
temporary slowdowns do not each need an update.

You may restructure the performance tables, add columns, or add more tables.
Include enough context to understand what the measurements compare.

Leave the rest of the README to the maintainer unless something is factually
incorrect. Do not create additional documentation unless requested.

Below the performance tables, include a remark of 1–3 sentences. Interpret
the results and give a candid reaction grounded in the measurements.

| Avoid | Prefer |
|---|---|
| These groundbreaking results demonstrate the tremendous potential of our architecture. | Arithmetic seems quite fast, which gives me hope that other workloads can improve too. Objects and any operations still need substantial optimization. |
| Scalar replacement improves deabstraction across the managed execution pipeline. | Temporary objects no longer allocate in this benchmark. That’s encouraging, though V8 is still faster here. |

Avoid hype and unexplained jargon. Say what the results suggest and why they
are encouraging or disappointing.

## Commit messages

Make focused commits with plain, readable titles. Assume the reader knows
the repository’s purpose but has not followed the conversation.

Name the concrete change. Short titles are usually sufficient, but longer
titles are fine when they improve clarity. Length itself is not the problem.

| Avoid | Prefer |
|---|---|
| Add replaceable Native AOT assembly writer and shared runtime | C# assembly writer |
| Use portable Crust standard-library imports | Use angle bracket imports |
| Export checked TypeScript IR with owned native snapshots | Pass TypeScript compiler results to Crust |
| Lower TypeScript to IL in Crust and emit assemblies directly | Compile directly to IL |
| Extend statement lowering with switch dispatch and parameter initialization | Support switch statements and default parameters |
| Verify default-argument evaluation and nested-loop control flow | Create tests for default arguments and nested loops |
| Make calls to Microsoft’s compiler safe | Remove unsafe code in calls to Microsoft’s compiler |

The preferred titles tell the reader what changed without making them decode
internal terminology. Removing unsafe code is also a more precise claim than
making code safe.

“Fix bugs” and “Update tests” are understandable, but too vague to be good
defaults. Name the bug or the behavior being tested.

Use “Initial commit” for an initial project import when that describes the
commit adequately.

### Commit bodies

Extended descriptions are rarely necessary. When a body is useful, explain
the reason for a decision or a consequence that the title does not convey.

For the title “C# assembly writer”:

| Avoid | Prefer |
|---|---|
| Uses Native AOT. | Uses Native AOT so the Crust compiler can link with it as a native library. |

The preferred body explains why Native AOT is used. The other merely names
the technology.

Omit the body when the title already explains the change sufficiently.
