# When project sources disagree

A project's own documentation often contradicts itself, or contradicts what its scenes and assets
actually ship. The user usually does not know. Treat a disagreement as a finding, not as noise to
resolve silently.

1. **Name it.** State both values and where each came from - which document, which scene or
   asset. A conflict the user cannot see is a conflict they cannot fix.
2. **Ask only if it changes what they will see.** Colour, brightness, whether shadows exist, time
   of day, which sky, or whether a surface is opaque are worth one question. Ask it as a single
   question with the actual options and what each one costs, never open-ended. Do NOT ask about a
   difference with no visible effect; pick one and move on, because a skill that stops on every
   ambiguity is worse than one that guesses on trivia.
3. **If you cannot ask, or it is not material, apply this precedence and say you did:** the
   written direction states intent and wins; a value shipped in a scene or asset tells you what the
   project actually renders. Where they differ, follow the written direction and report the
   shipped value as either drift to fix or an undocumented decision.
4. **Offer to record the answer.** Once a conflict is resolved, offer to write the resolution
   into the project's own documentation so the next person does not rediscover it. You MUST NOT
   edit the user's documentation without them agreeing to it first.

Do this because an unrecorded conflict is rediscovered on every task, and a silent choice is
unreviewable - nobody can tell a considered decision from a coin toss.
