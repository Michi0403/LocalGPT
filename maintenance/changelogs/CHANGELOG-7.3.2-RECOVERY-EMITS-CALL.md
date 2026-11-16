# LocalGPT 7.3.2

- gemma4:26b prepared the full codegen.review.create call in its thinking, ran out of output before emitting it, and the final-answer recovery then asked for a "final answer", so it wrote the plan as prose and no review was created. The recovery prompt now tells the model to emit the exact <localgpt-dx-call> with full parameters when the step exists to call a DXFunction.
