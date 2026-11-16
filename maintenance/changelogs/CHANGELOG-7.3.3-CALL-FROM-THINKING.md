# LocalGPT 7.3.3

- gemma4:26b wrote the complete <localgpt-dx-call> for codegen.review.create inside its thinking channel and emitted no answer, so the call was never executed (run a1b57978, X revision 2). When the answer is empty and the thinking contains a complete call tag, LocalGPT now routes that call through the normal function registry and approval policy.
