# Body-box coordinate migration

The archive's attack boxes use center coordinates, while the base body/push boxes encode x=-width/2,y=0 at the feet. Applying center semantics to the body leaves most of the torso unhittable. The content loader normalizes only these legacy body/push boxes to center coordinates (x += width/2, y += height/2). Attack boxes retain their declared centers. Canonical input JSON remains preserved. Production contact tests cover the resulting standing/crouching intersection; this changes collision consistency, not prices or currency identity.
