import torch
from pathlib import Path
import torchsummary

model_path = Path.cwd() / 'results\VisualFoodCollector_round3\VisualFoodCollector\VisualFoodCollector-16759939.pt'

if not model_path.exists():
    raise Exception("Couldnt find model")

actor_model = torch.load(model_path)
summary = torchsummary.summary(actor_model, input_size=(3, 84, 84))

print(summary)

dodo = True
